using System;

namespace AngerFootUltrawide
{
    // Pure maths, no Unity types, so it can be unit-tested off the game.
    public static class FovMath
    {
        public const double Baseline = 75.0;      // PlayerCamera.BASELINE_FOV (vertical degrees)
        const double D2R = Math.PI / 180.0;

        // Unity Camera.fieldOfView is VERTICAL. The game writes the slider straight into it,
        // so at 32:9 the world is already Hor+. The weapon camera keeps its prefab FOV forever.
        //
        // origWeapon: weapon camera's own FOV (prefab value)
        // slider:     GameSettings.FieldOfView (vertical degrees, 30-120)
        // strength:   0 = original game behaviour, 1 = gun shrinks/grows exactly like the world
        // aspect:     screen aspect; match16x9 = also compensate width so the gun sits where it would at 16:9
        // offset:     extra degrees on top (negative = bigger gun)
        public static double WeaponFov(double origWeapon, double slider, double strength,
                                       double aspect, bool match16x9, double offset)
        {
            if (origWeapon <= 0 || origWeapon >= 179) return origWeapon;
            strength = Clamp(strength, 0, 2);
            double tw = Math.Tan(origWeapon * 0.5 * D2R);
            double ratio = Math.Tan(Clamp(slider, 1, 170) * 0.5 * D2R) / Math.Tan(Baseline * 0.5 * D2R);
            tw *= Math.Pow(ratio, strength);
            if (match16x9 && aspect > 16.0 / 9.0 + 1e-4)
                tw *= aspect / (16.0 / 9.0);
            double fov = 2.0 * Math.Atan(tw) / D2R + offset;
            return Clamp(fov, 5, 170);
        }

        public static double HorizontalFov(double vfov, double aspect)
        {
            return 2.0 * Math.Atan(Math.Tan(vfov * 0.5 * D2R) * aspect) / D2R;
        }

        public static double Clamp(double v, double lo, double hi) { return v < lo ? lo : (v > hi ? hi : v); }
    }
}
