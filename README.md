# BattBar

Tray-resident utility that shows each battery of a Dell Latitude 5430 Rugged as an independent numeric badge in the Windows 11 notification area.

Built with C# / .NET 8 / WinForms. Cross-compiled on Linux to `win-x64 self-contained`. Manual install (no installer).

## Requirements

- For development: .NET 8 SDK (`dotnet --list-sdks` reports `8.x`)
- For the target laptop: Windows 11 (any 24H2 build), Dell Latitude 5430 Rugged
- No .NET runtime required on the laptop (the published .exe is self-contained)
- No admin rights required on the laptop (we only write to `HKCU`)
- No network access required at any point

## Development

```bash
# Restore dependencies
dotnet restore

# Build everything
dotnet build

# Run unit tests (Linux-runnable subset; tests WinForms are excluded automatically)
dotnet test

# Build the shippable single .exe
./build/publish.sh
# -> dist/BattBar.exe
```

## Install on the laptop

1. Copy `dist/BattBar.exe` to the laptop.
2. Double-click it.
3. On first run, two registry values get written (idempotent):
   - `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\EnableAutoTray=0` (show all tray icons)
   - `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\battbar = "<path>\BattBar.exe"` (autostart)
4. Tray icons appear in the notification area.
5. Right-click → Quit to leave it running.

### Tray icon legibility

By default Windows 11 shows tray icons at 16×16 pixels. The numeric badge inside BattBar's icons is legible at that size for two-digit numbers but small. To make icons easier to read:

- **Settings → Personalization → Taskbar → "Combine taskbar buttons and hide labels"** and pick "Always" or "When taskbar is full" (cosmetic only)
- **Settings → Accessibility → Display → "Make text bigger"** — applies to the tray
- **Settings → System → Display → Scale** (e.g., 125%, 150%, 175%) — scales everything in the tray, including BattBar's icons

BattBar is per-monitor DPI-aware, so the 32×32 badge renders at the user's actual display scale and stays sharp at 125%, 150%, and 175%.

## Definition of done

See [`SMOKE_CHECKLIST.md`](SMOKE_CHECKLIST.md). Every checkbox must pass before v1 ships.

## Uninstall

1. Quit BattBar.
2. Untickeright-click "Run on startup" (or delete `HKCU\...\Run\battbar` manually).
3. Delete `BattBar.exe`.
4. Optionally reset `EnableAutoTray=1` in Taskbar settings if you want the overflow arrow back.

## Logs

- `%LOCALAPPDATA%\BattBar\error.log` — registry failures and unexpected exceptions only.
- No logging in the happy path.

## Files

```
battbar/
├── src/                  Source code (Core/ for data, Tray/ for UI)
├── tests/                xUnit unit tests (Linux-runnable subset only)
├── build/publish.sh      Cross-compile to win-x64 self-contained
├── dist/BattBar.exe      Build output (.gitignored)
├── SMOKE_CHECKLIST.md    Definition of done for v1
├── global.json           Pinned .NET SDK version
└── README.md             This file
```

## Architecture reference

The complete design lives at `docs/superpowers/specs/2026-07-22-battbar-design.md` in the workspace root.