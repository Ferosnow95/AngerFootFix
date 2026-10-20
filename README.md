# Anger Foot — Ultrawide Viewmodel FOV Fix

[![Downloads](https://img.shields.io/github/downloads/Ferosnow95/AngerFootFix/total.svg)](https://github.com/Ferosnow95/AngerFootFix/releases)
[![Latest release](https://img.shields.io/github/v/release/Ferosnow95/AngerFootFix?include_prereleases)](https://github.com/Ferosnow95/AngerFootFix/releases)

A BepInEx 5 plugin that makes the hands and guns in Anger Foot follow the in-game **Field Of View** slider, so they look right at high FOV on ultrawide and super-ultrawide screens (21:9, 32:9 and wider).

Windows only. Single-player game, no anti-cheat.

| After (this fix, 32:9, FOV 120) |
|---|
| ![After](docs/after.jpg) |

## Features
- **Viewmodel follows the FOV slider.** The hands and guns shrink or grow exactly like the world does when you change the FOV.
- **Live tuning keys** (F5/F6) for the viewmodel size, saved automatically.
- **Optional 16:9 placement:** pushes the hands out towards the screen edges like they sit on a 16:9 screen.
- **Leaves the game's zooms alone:** aim-down-sights and scripted camera zooms behave as before.
- **One-key A/B toggle** (F8) and a log file for troubleshooting.

## Requirements
- Anger Foot (Steam). The game runs on Unity **2019.4.35f1** (Mono).
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) x64. The release zip already includes BepInEx **5.4.23.2**.

## Install
### Release
1. Download the latest zip from [Releases](https://github.com/Ferosnow95/AngerFootFix/releases).
2. Extract it into the game folder, next to `Anger Foot.exe`. You should get `winhttp.dll`, `doorstop_config.ini` and a `BepInEx` folder.
3. Launch the game. BepInEx creates `BepInEx\config\ferosnow.angerfoot.ultrawide.cfg` on the first run.

Steam Deck / Linux (Proton): set the launch option `WINEDLLOVERRIDES="winhttp=n,b" %command%`.

### From source
1. Install BepInEx 5 x64 into the game folder and run the game once.
2. Build: `dotnet build src/AngerFootUltrawide -c Release -p:GameDir="<path to Anger Foot>"`
3. Copy `AngerFootUltrawide.dll` into `Anger Foot\BepInEx\plugins\AngerFootUltrawide\`.

The maths has its own tests: `dotnet run --project tests/FovMathTests`.

## Controls
| Key | Action |
|---|---|
| F5 / F6 | Viewmodel FOV -1° / +1° (bigger / smaller hands and guns). Saved automatically. |
| F8 | Toggle the fix on/off (restores the game's own viewmodel) |
| F7 | Write the camera values to the log |

## Configuration
Adjust settings in `Anger Foot\BepInEx\config\ferosnow.angerfoot.ultrawide.cfg`.

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | `true` | Turns the fix on. Same as F8. |
| `FollowStrength` | `1` | How much the viewmodel follows the FOV slider. `1` = exactly like the world, `0` = the original game, `0.5` = halfway. |
| `ViewmodelFovOffset` | `0` | Extra viewmodel FOV in degrees. Positive = smaller hands/guns, negative = bigger. F5/F6 change it. |
| `Match16x9Placement` | `false` | Also widens the viewmodel for your aspect ratio, so the hands sit where they would across a 16:9 screen (out towards the edges at 32:9). The hands and guns get smaller. |
| `VerboseLog` | `false` | More logging. |
| `Toggle` / `Dump` / `OffsetDown` / `OffsetUp` | `F8` / `F7` / `F5` / `F6` | Key bindings. |

## How it works
Anger Foot draws the world and the hands with two separate Unity cameras inside `PlayerCamera`: `_mainCamera` (plus an effects camera and the blood decal camera) and `_weaponCamera`.

Every frame, `PlayerCamera.ApplyFOV()` writes the FOV slider (`GameSettings.FieldOfView`, 30–120, default 75) to the world cameras. Unity's `fieldOfView` is **vertical**, so the world is already Hor+: on 32:9 the default 75° is about 140° wide, and 120° is about 162° wide. The weapon camera is never touched. It keeps the **60°** it was built with, so at a high FOV the world zooms out while the hands and guns stay huge.

The fix is a Harmony postfix on `PlayerCamera.ApplyFOV()`. It records the weapon camera's own FOV once, then sets it every frame from that original value, so nothing compounds:

```
tan(weapon / 2) = tan(original / 2) × ( tan(slider / 2) / tan(75 / 2) ) ^ FollowStrength
```

75° is the game's own baseline (`PlayerCamera.BASELINE_FOV`). At 75 nothing changes. At 120 the weapon camera goes from 60° to 105°, so the hands scale by exactly the same amount as the world.

`Match16x9Placement` multiplies the same tangent by `aspect / (16/9)`, so a hand at a given horizontal spot on a 16:9 screen lands at the same fraction of the width on yours.

The plugin follows the **slider**, not the live camera FOV, so aim-down-sights (8°) and scripted look-at zooms keep the viewmodel the game intended. The game's own conversion from weapon-camera space to world space (`ConvertFromWeaponCameraSpaceToMainCameraSpace`, used for shell ejection) goes through the viewport, so it stays correct.

## Known issues
- Hands and guns are one rig that sits near the centre of the screen. The fix scales them with the FOV; it doesn't move each hand separately. Use `Match16x9Placement` if you want them further out.
- Developed and tested on one PC (32:9, 5120×1440). Reports from 21:9 and 48:9 (triple-screen) setups are welcome.

## Troubleshooting
The plugin writes `BepInEx\plugins\AngerFootUltrawide\AngerFootUltrawide.log` (the previous session is kept as `AngerFootUltrawide.prev.log`). Each `RESULT` line shows the slider value and the viewmodel FOV it became. If the plugin doesn't load at all, check `BepInEx\LogOutput.log`. Press F7 in game and attach both logs to an issue.

To uninstall, delete `winhttp.dll` (disables BepInEx) or the `BepInEx\plugins\AngerFootUltrawide` folder.

## Changelog
See [CHANGELOG.md](CHANGELOG.md).

## License
Distributed under the MIT License. See [LICENSE](LICENSE).

## External tools and credits
- [BepInEx](https://github.com/BepInEx/BepInEx) (LGPL-2.1) and [HarmonyX](https://github.com/BepInEx/HarmonyX) — the plugin loader and patching library. BepInEx 5.4.23.2 is included unchanged in the release zip.
- [ILSpy](https://github.com/icsharpcode/ILSpy) — used to read the game's camera code.
- [PolarWizard/CodeVeinFix](https://github.com/PolarWizard/CodeVeinFix) — README format.
