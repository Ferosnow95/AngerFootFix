# Changelog

## v0.1 — 2026-10-19 (release)
**First release: the hands and guns follow the FOV slider.**
- Harmony postfix on `PlayerCamera.ApplyFOV()` sets the weapon camera's FOV from the slider every frame, always starting from its original 60°, so values never compound.
- `FollowStrength` (default 1), `ViewmodelFovOffset` (default 0) and `Match16x9Placement` (default off) settings.
- F5/F6 tune the viewmodel FOV live and save it; F8 toggles the fix; F7 writes the camera values to the log.
- Own log file with a `RESULT` line each time the slider or offset changes.
- Aim-down-sights and scripted camera zooms are left as the game made them.
- Confirmed in game on 32:9 (5120×1440) at FOV 120: viewmodel 60° → 105°, stable across level loads.
