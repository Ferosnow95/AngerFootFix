using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Framework;

namespace AngerFootUltrawide
{
    [BepInPlugin(Guid, "Anger Foot Ultrawide Viewmodel Fix", Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "ferosnow.angerfoot.ultrawide";
        public const string Version = "0.1.0";

        internal static Plugin I;
        internal static ConfigEntry<bool> Enabled, Match16x9, Verbose;
        internal static ConfigEntry<float> Strength, Offset;
        internal static ConfigEntry<KeyCode> KeyToggle, KeyDump, KeyMinus, KeyPlus;

        // weapon camera instance id -> its original (prefab) FOV. Only plain values are kept.
        static readonly Dictionary<int, float> orig = new Dictionary<int, float>();
        static string lastState = "";
        static StreamWriter log;
        static float lastWarn;

        void Awake()
        {
            I = this;
            Enabled  = Config.Bind("General", "Enabled", true, "Make the gun/hand viewmodel follow the Field Of View slider. F8 toggles in game.");
            Strength = Config.Bind("General", "FollowStrength", 1.0f, new ConfigDescription(
                "How much the viewmodel follows the FOV slider. 0 = original game (gun never changes), 1 = gun scales exactly like the world, 0.5 = halfway.", new AcceptableValueRange<float>(0f, 2f)));
            Offset   = Config.Bind("General", "ViewmodelFovOffset", 0f, new ConfigDescription(
                "Extra viewmodel FOV in degrees on top of the above. Positive = smaller gun / more spread, negative = bigger gun. F5/F6 change it live.", new AcceptableValueRange<float>(-40f, 60f)));
            Match16x9 = Config.Bind("Ultrawide", "Match16x9Placement", false,
                "Also widen the viewmodel for your aspect ratio, so the hands sit at the same place across the screen as on 16:9 (towards the edges at 32:9). The gun gets smaller as a result. Off = guns keep their size and stay near the centre.");
            Verbose  = Config.Bind("Debug", "VerboseLog", false, "Log every value change.");
            KeyToggle = Config.Bind("Keys", "Toggle", KeyCode.F8, "Toggle the fix on/off (restores the game's own viewmodel FOV).");
            KeyDump   = Config.Bind("Keys", "Dump", KeyCode.F7, "Write camera info to the log.");
            KeyMinus  = Config.Bind("Keys", "OffsetDown", KeyCode.F5, "Viewmodel FOV offset -1 (bigger gun).");
            KeyPlus   = Config.Bind("Keys", "OffsetUp", KeyCode.F6, "Viewmodel FOV offset +1 (smaller gun).");

            OpenLog();
            Log("AngerFootUltrawide " + Version + " loaded. Unity " + Application.unityVersion);
            try
            {
                new Harmony(Guid).PatchAll(typeof(Patches));
                Log("Hooked PlayerCamera.ApplyFOV");
            }
            catch (Exception e) { Log("PATCH FAILED: " + e); }
        }

        void OpenLog()
        {
            try
            {
                string dir = Path.GetDirectoryName(Info.Location);
                string p = Path.Combine(dir, "AngerFootUltrawide.log");
                string prev = Path.Combine(dir, "AngerFootUltrawide.prev.log");
                if (File.Exists(p)) { if (File.Exists(prev)) File.Delete(prev); File.Move(p, prev); }
                log = new StreamWriter(p, false) { AutoFlush = true };
            }
            catch { log = null; }
        }

        internal static void Log(string s)
        {
            if (I != null) I.Logger.LogInfo(s);
            try { if (log != null) log.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + s); } catch { }
        }

        void Update()
        {
            try
            {
                if (Input.GetKeyDown(KeyToggle.Value))
                {
                    Enabled.Value = !Enabled.Value;
                    Log("Toggle -> " + (Enabled.Value ? "ON" : "OFF (game original)"));
                    if (!Enabled.Value) RestoreAll();
                }
                if (Input.GetKeyDown(KeyMinus.Value)) { Offset.Value = Offset.Value - 1f; Log("ViewmodelFovOffset = " + Offset.Value.ToString("0")); }
                if (Input.GetKeyDown(KeyPlus.Value))  { Offset.Value = Offset.Value + 1f; Log("ViewmodelFovOffset = " + Offset.Value.ToString("0")); }
                if (Input.GetKeyDown(KeyDump.Value)) Dump();
            }
            catch (Exception e) { Warn("Update: " + e.Message); }
        }

        static void Warn(string s)
        {
            if (Time.unscaledTime - lastWarn < 5f) return;
            lastWarn = Time.unscaledTime;
            Log("ERROR " + s);
        }

        static void RestoreAll()
        {
            try
            {
                if (!SingletonBehaviour<PlayerCamera>.HasInstance) return;
                Camera wc = SingletonBehaviour<PlayerCamera>.Instance.WeaponCamera;
                float o;
                if (wc != null && orig.TryGetValue(wc.GetInstanceID(), out o)) wc.fieldOfView = o;
            }
            catch (Exception e) { Warn("Restore: " + e.Message); }
        }

        static float Aspect(Camera c)
        {
            float a = c != null ? c.aspect : 0f;
            if (a <= 0f && Screen.height > 0) a = (float)Screen.width / Screen.height;
            return a;
        }

        // Runs right after the game's own ApplyFOV (every LateUpdate).
        internal static void AfterApplyFov(PlayerCamera pc)
        {
            Camera wc = pc.WeaponCamera;
            if (wc == null) return;
            int id = wc.GetInstanceID();
            float o;
            if (!orig.TryGetValue(id, out o))
            {
                if (orig.Count > 32) orig.Clear();   // new level = new camera; never grows unbounded
                o = wc.fieldOfView;
                orig[id] = o;
                Log("New weapon camera: original FOV " + o.ToString("0.00") + " (main " + pc.MainCamera.fieldOfView.ToString("0.0") + ", aspect " + Aspect(wc).ToString("0.000") + ")");
            }
            if (!Enabled.Value) { if (wc.fieldOfView != o) wc.fieldOfView = o; return; }

            float slider = GameSettings.FieldOfView;
            float a = Aspect(wc);
            float target = (float)FovMath.WeaponFov(o, slider, Strength.Value, a, Match16x9.Value, Offset.Value);
            wc.fieldOfView = target;   // always written from the original, so nothing compounds

            string st = slider + "|" + target.ToString("0.0") + "|" + a.ToString("0.000");
            if (st != lastState)
            {
                lastState = st;
                Log("RESULT slider " + slider + " (" + FovMath.HorizontalFov(slider, a).ToString("0") + " deg horizontal at " + a.ToString("0.000") + ") -> viewmodel " +
                    o.ToString("0.0") + " => " + target.ToString("0.0") + " (strength " + Strength.Value.ToString("0.00") + ", offset " + Offset.Value.ToString("0") + (Match16x9.Value ? ", 16:9 placement" : "") + ")");
            }
        }

        static void Dump()
        {
            Log("---- F7 dump ----");
            Log("Enabled " + Enabled.Value + "  slider " + GameSettings.FieldOfView + "  screen " + Screen.width + "x" + Screen.height);
            if (!SingletonBehaviour<PlayerCamera>.HasInstance) { Log("No PlayerCamera in this scene"); return; }
            PlayerCamera pc = SingletonBehaviour<PlayerCamera>.Instance;
            foreach (var pair in new[] { new KeyValuePair<string, Camera>("main", pc.MainCamera), new KeyValuePair<string, Camera>("effects", pc.EffectsCamera), new KeyValuePair<string, Camera>("weapon", pc.WeaponCamera) })
            {
                Camera c = pair.Value;
                if (c == null) { Log(pair.Key + ": null"); continue; }
                Log(pair.Key + ": '" + c.name + "' vfov " + c.fieldOfView.ToString("0.00") + " hfov " + FovMath.HorizontalFov(c.fieldOfView, c.aspect).ToString("0.0") +
                    " aspect " + c.aspect.ToString("0.000") + " depth " + c.depth + " near " + c.nearClipPlane + " mask 0x" + c.cullingMask.ToString("X8") + " rect " + c.rect + " enabled " + c.enabled);
            }
            float o;
            if (pc.WeaponCamera != null && orig.TryGetValue(pc.WeaponCamera.GetInstanceID(), out o)) Log("weapon original FOV " + o.ToString("0.00"));
        }
    }

    [HarmonyPatch]
    static class Patches
    {
        [HarmonyPatch(typeof(PlayerCamera), "ApplyFOV")]
        [HarmonyPostfix]
        static void ApplyFovPostfix(PlayerCamera __instance)
        {
            try { Plugin.AfterApplyFov(__instance); }
            catch (Exception e) { Plugin.Log("ERROR ApplyFOV postfix: " + e.Message); }
        }
    }
}
