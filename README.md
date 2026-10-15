# SPX Webcast Overlay

A Falcon 9 webcast-style telemetry overlay for **Kerbal Space Program 1.12.x**. Hide the UI (F2) during flight and get a launch-broadcast-style bottom bar: countdown/MET clock, mission name, event timeline, speed/altitude/G gauges and an engine-status gauge.

*Unofficial fan project. Not affiliated with or endorsed by SpaceX.*

## Screenshots
![SPX Webcast Overlay preview](screenshots/overlay_preview.png)

## Features
- T-/T+ clock with configurable countdown start, hold at T-0, auto-zero on liftoff
- Mission name under the clock (defaults to the vessel name)
- Scrolling event timeline (STARTUP, LIFTOFF, MAX-Q, MECO, STAGE SEP, BOOSTBACK, ENTRY BURN, LANDING BURN) — every event editable/toggleable, plus custom events; optional static full-timeline view
- Anti-aliased speed / altitude / G gauges with configurable maxima
- ENGINES gauge showing lit engines and remaining booster propellant (Tundra Exploration Falcon 9)
- Smooth fade in/out when toggling F2; preview mode while the UI is visible
- Settings window from the toolbar / app launcher; global settings with optional per-save settings
- Optional kOS integration: `ADDONS:WCOL` (`COUNTDOWNFROM(n)`, `HOLD()`, `RESUME()`, `RESET()`, `SETMISSION("..")`, `SHOW()`, `HIDE()`, `MET`, `AVAILABLE`)

## Requirements
- KSP 1.12.x
- [ToolbarController](https://github.com/linuxgurugamer/ToolbarControl) (CKAN: `ToolbarController`)
- [ClickThroughBlocker](https://github.com/linuxgurugamer/ClickThroughBlocker) (CKAN: `ClickThroughBlocker`)

### Recommended
- **Tundra Exploration** (CKAN: `TundraExploration`) — the overlay works without it, but not properly: the ENGINES gauge only appears for the Tundra Falcon 9 booster (or engines with matching engine modes).
- **kOS** (CKAN: `kOS`) — optional, for scripting the clock.

## Installation
**CKAN:** search for *SPX Webcast Overlay*.
**Manual:** download `SPXWebcastOverlay-<version>.zip` from Releases/SpaceDock and merge its `GameData` folder into your KSP `GameData`. You should end up with `GameData/SPXWebcastOverlay/`.

## Usage
Open settings from the toolbar button. In flight press **F2** to show the overlay. See `GameData/SPXWebcastOverlay/README.txt` for details and a kOS example.

## Building
Targets .NET Framework 4.8 (`dotnet build -c Release`). Put KSP/Unity refs (Assembly-CSharp, UnityEngine*, ToolbarControl, ClickThroughBlocker) in `refs/` and kOS.dll / kOS.Safe.dll in `refs_kos/` (not distributed).

## License
Code: MIT (see `LICENSE`). Font atlases: SIL OFL 1.1, derived from D-DIN by Datto Inc. (`GameData/SPXWebcastOverlay/Fonts/OFL.txt`).
