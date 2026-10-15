# Changelog

## 1.7.0 — first public release
- Fix: ENGINES gauge now detects Tundra Exploration's Falcon 9 octaweb (runtime part name `TE.19.F9.S1.Engine`), with fallback to any part with ModuleEnginesFX `AllEngines`/`ThreeLanding`/`CenterOnly` modes. Re-scans on vessel change/modify/staging.
- Optional kOS integration (`Plugins/kOS.WCOL.dll`, `ADDONS:WCOL:*`): countdown, hold/resume, reset, mission name, show/hide, MET.

## 1.6
- All built-in timeline events can be disabled; custom events (name, T+ time, on/off, remove), "Reset events to defaults". Settings v3 with migration. KSP skin for settings window, new toolbar icon.

## 1.5
- Layout matched to webcast reference frames; D-DIN-derived bitmap fonts (OFL 1.1); timeline scale ~1.5 px/s; engine icon ring layout.

## 1.4
- Re-measured bottom band styling, centre gradient under the clock, repositioned elements.

## 1.3
- Centre strip fade behind clock; "Mission name" setting.

## 1.2
- Shaped dark band with sloped ends behind the gauge pairs.

## 1.1
- Bitmap font atlases, anti-aliased arcs, smooth animation, glowing engines, F2 fade.

## 1.0
- Initial version (as F9Webcast): telemetry bar, countdown clock, timeline, speed/altitude/G gauges.
