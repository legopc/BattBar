# BattBar Smoke Checklist

> **This is the definition of done for BattBar v1. Every item MUST pass before ship.**

## Pre-flight

- [ ] `dist/BattBar.exe` exists and is > 20 MB
- [ ] Laptop is Windows 11 (any 24H2 build) on a Dell Latitude 5430 Rugged
- [ ] Both battery bays have functional batteries

## On the laptop

1. - [ ] Cold boot, both batteries present → two icons appear in the tray cluster, percentages correct
2. - [ ] Pull battery 1 → tray icon 1 turns to `—` within ~1s; tooltip reads "Battery 1: not present"
3. - [ ] Re-insert battery 1 → tray icon 1 returns with current percent within ~1s
4. - [ ] Pull battery 2 → same as #2 for icon 2
5. - [ ] Quick hot-swap sequence (pull 1, insert 1, pull 2, insert 2 within 5s) → all icons correct, no ghosts
6. - [ ] Plug in AC → both icons turn green, tooltip reads "Charging"
7. - [ ] Unplug AC → both icons return to blue, tooltip reads "Discharging"
8. - [ ] Battery below 5% → icon turns red, tooltip says "Critical"
9. - [ ] External monitor connect → icons do not jump, do not disappear
10. - [ ] External monitor disconnect → icons do not jump, do not disappear
11. - [ ] DPI change (Settings → Display → Scale) → icons re-render at correct size
12. - [ ] Taskbar move to top → icons reposition correctly
13. - [ ] Taskbar move to left → icons reposition correctly
14. - [ ] Reboot → app starts automatically (autostart works); two icons appear
15. - [ ] Quit via right-click → app exits cleanly; tray icons disappear
16. - [ ] Autostart registry entry survives quit (`HKCU\...\Run\battbar` still present)
17. - [ ] Run `BattBar.exe` twice quickly → second instance exits silently; first keeps running
18. - [ ] Right-click "Run on startup" uncheck → registry value removed
19. - [ ] Right-click "Run on startup" recheck → registry value added
20. - [ ] Reboot after re-checking autostart → app starts automatically
21. - [ ] Right-click → About → shows correct version and .exe path
22. - [ ] Right-click → Open Windows Battery Settings → opens Settings → System → Battery
23. - [ ] One-week survival: app continues to function after varied daily use (hot-swap, AC, monitors, DPI)

## Uninstall test

- [ ] Quit BattBar
- [ ] Right-click "Run on startup" is already off (or untick it now)
- [ ] Delete `BattBar.exe`
- [ ] Confirm `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\battbar` is gone
- [ ] Confirm `HKCU\Software\BattBar\pid` is gone
- [ ] Optionally: reset `HKCU\...\Explorer\Advanced\EnableAutoTray` to `1` via Taskbar settings

If ANY checkbox above fails, it is a bug. Do not declare v1 done.