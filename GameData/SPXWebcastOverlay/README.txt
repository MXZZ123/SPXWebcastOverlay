SPXWebcastOverlay 1.7.0 (formerly F9Webcast) - SpaceX Falcon 9 webcast-style booster telemetry bar for KSP 1.x
Requires: ToolbarControl, ClickThroughBlocker.
Overlay enabled: big ON/OFF button at the top of the settings window (default ON). When OFF the HUD never draws, even with F2 or Preview.
Usage: in flight press F2 (hide UI) to show the bar. Open settings from the toolbar/app-launcher "SPX Webcast Overlay" button.
- Start Countdown: starts T- from the configured countdown start. Reset clears the clock.
- If you launch before starting the countdown (or with "auto-zero on liftoff"), the clock goes to T+0 at liftoff.
- "Hold at T-0" holds the clock at 00:00:00 until liftoff; otherwise it keeps counting up.
- ENGINES gauge appears only with Tundra Exploration's TE_19_F9_S1_Engine; arc = booster LF+OX remaining.
- Event times, gauge maxima, Preview HUD toggle. Settings: GameData/SPXWebcastOverlay/PluginData/settings.cfg
  (optionally also saves/<save>/SPXWebcastOverlay.cfg).
Timeline (default, dynamic): a bright "now" dot sits fixed at the centre of the timeline; event markers
scroll right-to-left to meet it. Window = current time +/- half the span: 180 s before the zoom, eased
(~1.5 s) to 60 s from T-6. Past line is bright, future line dim; passed markers are filled and fade out at
the left edge, upcoming ones fade in at the right. Labels stagger above/below; an earlier passed label is
hidden if they'd still overlap. Settings: zoom trigger (s before T-0), pre-zoom span, zoomed span,
"Static full timeline" (old STARTUP..LANDING+30s view, off by default).
Look (v1.1): bitmap font atlases (Fonts/; Unity default font is used if missing), anti-aliased arcs with dim track + bright value arc on a dark disc, smooth
value animation, glowing lit engines, now-dot halo, letter-spaced labels, small "T+" with large clock,
smooth 75% gradient, and a 0.4 s fade in/out when F2 toggles the UI.
Band (v1.2): shaped dark band like the webcast - tall ends (14.5% of screen height) behind the gauge
pairs, ~40 deg anti-aliased slopes down to a low centre strip (4.3% of height) under the clock, over a
soft bottom gradient. Gauges, timeline and clock positioned from the reference frame proportions.
v1.3: the band's low centre strip fades out horizontally (clear ~170 px either side of centre at 1080p,
smooth 160 px feather) behind the clock and mission name. "Mission name" field in settings (empty = vessel
name; shown uppercase); saved in settings.cfg / per save, editable any time incl. in flight.
v1.4: bottom styling re-measured from 4K webcast frames: end bands (hard top edge 181 px above bottom
at 1080p, ~38% dark, flat to x=370, ~39 deg slope to (490, bottom-82) fading out); no centre strip.
Instead a vertical gradient (from ~170 px above bottom, roughly linear) that is darkest in the centre
under the clock (~85%) and lighter toward the screen edges (~36%). Clock/name/timeline/gauges
repositioned to the measured layout.
v1.5: matched to 4K webcast frames.
- Fonts: bitmap atlases rendered from Datto's D-DIN family (SIL OFL 1.1, Fonts/OFL.txt): spx_regular =
  D-DIN Exp (clock digits, gauge values), spx_bold = D-DIN Bold (labels, units, T+, timeline, mission name).
  The atlases are derived renderings and are not named D-DIN (reserved font name).
- Text cap heights at 1080p: clock digits 34.5, "T+" 22, gauge values 27, gauge labels 10.5, units 9.5,
  timeline labels 11, mission name 11.
- Timeline scale ~1.5 px/s at 1080p (no zoom in the real webcast): default spans 640 s pre-T-6, 600 s after.
  Settings saved by older versions are migrated once (settingsVersion 2) to the new span defaults.
- Engines icon: 8 dots on a ring at 22.5+45n deg (no dot at the top) + centre; lit solid white, unlit 27%
  white; 3-engine burn = centre + upper-right + lower-left. Gauges: 4 px track/value arc, thin inner ring.
v1.6: every built-in event can be switched off; custom events (name, T+ seconds, may be negative, on/off,
remove) with "Add event" and "Reset events to defaults" in a scrollable list. Saved as EVENT nodes
(settingsVersion 3; older settings are migrated keeping your times). Hiding LIFTOFF doesn't stop the clock
zeroing at liftoff. Settings window now uses KSP's own skin; new toolbar icon (Falcon 9 + timeline).
v1.7:
- Fix: ENGINES gauge never appeared. KSP stores part names with '_' turned into '.', so the Tundra octaweb is
  "TE.19.F9.S1.Engine" at runtime; detection now normalises names and falls back to any part carrying
  ModuleEnginesFX AllEngines/ThreeLanding/CenterOnly. Re-scans on vessel change/modify/staging and logs
  "[SPXWebcastOverlay] engines gauge: ..." to KSP.log.
- kOS integration (optional): Plugins/kOS.WCOL.dll (needs kOS; the overlay works without it).
  Suffixes (case-insensitive):
    ADDONS:WCOL:AVAILABLE          true when the overlay is loaded
    ADDONS:WCOL:COUNTDOWNSTART()   start countdown from the configured start (T-0 = now + start)
    ADDONS:WCOL:COUNTDOWNFROM(n)   start countdown from T-n
    ADDONS:WCOL:HOLD()  / RESUME() freeze / continue the clock
    ADDONS:WCOL:RESET()            reset the clock
    ADDONS:WCOL:SETMISSION("NAME") mission name under the clock
    ADDONS:WCOL:SHOW()  / HIDE()   overlay enabled on / off
    ADDONS:WCOL:MET                seconds relative to T-0 (negative before)
  Liftoff auto-sync still applies (the clock zeroes at liftoff).
  Example:
    if ADDONS:AVAILABLE("WCOL") {
      ADDONS:WCOL:SETMISSION("STARLINK 12-7").
      ADDONS:WCOL:COUNTDOWNFROM(20).
      wait until ADDONS:WCOL:MET >= -3.
      stage.                         // ignition
      wait until ADDONS:WCOL:MET >= 0.
      stage.                         // release clamps -> liftoff
    }
