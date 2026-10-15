using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KSP.UI.Screens;
using ToolbarControl_NS;
using ClickThroughFix;

[assembly: KSPAssembly("SPXWebcastOverlay", 1, 2)]

namespace SPXWebcastOverlay
{
    public class Ev
    {
        static int nextId;
        public readonly int id = ++nextId;
        public string name = "", key = ""; public double time; public bool enabled = true, builtin;
        public float alpha; public int row;           // runtime only
        public string Label { get { return (name ?? "").Trim().ToUpperInvariant(); } }
    }

    public class Settings
    {
        public static readonly string[] EventNames = { "STARTUP", "LIFTOFF", "MAX-Q", "MECO", "STAGE SEP", "BOOSTBACK", "ENTRY BURN", "LANDING BURN" };
        public static readonly string[] EventKeys = { "startup", "liftoff", "maxQ", "meco", "stageSep", "boostback", "entryBurn", "landingBurn" };
        public static readonly double[] EventDefaults = { -3, 0, 72, 145, 149, 160, 390, 480 };
        public double countdownStart = 10;
        public List<Ev> events = DefaultEvents();
        public static List<Ev> DefaultEvents()
        {
            var l = new List<Ev>();
            for (int i = 0; i < EventNames.Length; i++) l.Add(new Ev { name = EventNames[i], key = EventKeys[i], time = EventDefaults[i], builtin = true });
            return l;
        }
        public double maxSpeedKmh = 8000, maxAltKm = 100, maxG = 5;
        public bool overlayEnabled = true;
        public string missionName = "";
        public bool autoZeroOnLiftoff = true, holdAtZero = false, perSave = false, staticTimeline = false;
        public double zoomTrigger = 6, windowSpan = 600, preSpan = 640;
        public const int CurrentSettingsVersion = 3; public int settingsVersion = CurrentSettingsVersion;

        public static string GlobalPath { get { return Path.Combine(KSPUtil.ApplicationRootPath, "GameData/SPXWebcastOverlay/PluginData/settings.cfg"); } }
        static string SavePath { get { return HighLogic.SaveFolder == null ? null : Path.Combine(KSPUtil.ApplicationRootPath, "saves/" + HighLogic.SaveFolder + "/SPXWebcastOverlay.cfg"); } }

        public void Load()
        {
            Read(GlobalPath);
            if (perSave && SavePath != null && File.Exists(SavePath)) { Read(SavePath); perSave = true; }
        }
        void Read(string p)
        {
            if (!File.Exists(p)) return;
            ConfigNode n = ConfigNode.Load(p); if (n == null) return;
            if (n.HasNode("SPXWEBCASTOVERLAY")) n = n.GetNode("SPXWEBCASTOVERLAY");
            else if (n.HasNode("F9WEBCAST")) n = n.GetNode("F9WEBCAST");
            n.TryGetValue("countdownStart", ref countdownStart);
            n.TryGetValue("maxSpeedKmh", ref maxSpeedKmh);
            n.TryGetValue("maxAltKm", ref maxAltKm);
            n.TryGetValue("maxG", ref maxG);
            n.TryGetValue("autoZeroOnLiftoff", ref autoZeroOnLiftoff);
            n.TryGetValue("holdAtZero", ref holdAtZero);
            n.TryGetValue("overlayEnabled", ref overlayEnabled);
            n.TryGetValue("missionName", ref missionName); if (missionName == null) missionName = "";
            n.TryGetValue("perSave", ref perSave);
            n.TryGetValue("staticTimeline", ref staticTimeline);
            n.TryGetValue("zoomTrigger", ref zoomTrigger);
            n.TryGetValue("windowSpan", ref windowSpan);
            n.TryGetValue("preSpan", ref preSpan);
            int ver = 1; n.TryGetValue("settingsVersion", ref ver);
            if (ver < 2) { windowSpan = 600; preSpan = 640; }   // v2: spans re-measured from the real webcast
            settingsVersion = CurrentSettingsVersion;
            if (n.HasNode("EVENT"))
            {
                var l = new List<Ev>();
                foreach (ConfigNode e in n.GetNodes("EVENT"))
                {
                    var ev = new Ev(); e.TryGetValue("name", ref ev.name); e.TryGetValue("key", ref ev.key);
                    e.TryGetValue("time", ref ev.time); e.TryGetValue("enabled", ref ev.enabled); e.TryGetValue("builtin", ref ev.builtin);
                    if (ev.name == null) ev.name = ""; if (ev.key == null) ev.key = "";
                    l.Add(ev);
                }
                events = l;
            }
            else
            {
                // v3 migration: old per-event time fields -> EVENT list (times preserved)
                events = DefaultEvents();
                foreach (var ev in events) n.TryGetValue(ev.key, ref ev.time);
            }
        }
        public void Save()
        {
            ConfigNode root = new ConfigNode(); ConfigNode n = root.AddNode("SPXWEBCASTOVERLAY");
            n.AddValue("settingsVersion", CurrentSettingsVersion);
            n.AddValue("countdownStart", countdownStart);
            n.AddValue("maxSpeedKmh", maxSpeedKmh); n.AddValue("maxAltKm", maxAltKm); n.AddValue("maxG", maxG);
            n.AddValue("autoZeroOnLiftoff", autoZeroOnLiftoff); n.AddValue("holdAtZero", holdAtZero); n.AddValue("overlayEnabled", overlayEnabled); n.AddValue("missionName", missionName ?? "");
            n.AddValue("perSave", perSave); n.AddValue("staticTimeline", staticTimeline); n.AddValue("zoomTrigger", zoomTrigger); n.AddValue("windowSpan", windowSpan); n.AddValue("preSpan", preSpan);
            foreach (var ev in events)
            {
                ConfigNode e = n.AddNode("EVENT");
                e.AddValue("name", ev.name ?? ""); e.AddValue("time", ev.time); e.AddValue("enabled", ev.enabled);
                e.AddValue("builtin", ev.builtin); if (ev.builtin) e.AddValue("key", ev.key);
            }
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(GlobalPath));
                root.Save(GlobalPath);
                if (perSave && SavePath != null) root.Save(SavePath);
            }
            catch (Exception e) { Debug.LogError("[SPXWebcastOverlay] save failed: " + e); }
        }
    }

    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class SPXRegister : MonoBehaviour
    {
        void Start() { ToolbarControl.RegisterMod(OverlayAddon.MODID, OverlayAddon.MODNAME); }
    }

    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class OverlayAddon : MonoBehaviour
    {
        public const string MODID = "SPXWebcastOverlay_NS", MODNAME = "SPX Webcast Overlay";
        public static Settings S;
        ToolbarControl toolbar;
        bool showWindow, uiHidden, preview;
        Rect winRect = new Rect(200, 80, 400, 10);
        Dictionary<string, string> edit = new Dictionary<string, string>();

        // clock
        static double t0UT = double.NaN; static bool launched, held; static double heldT; Guid trackedVessel;
        public static OverlayAddon Instance;
        Material mat; GUIStyle sSmall;

        void Start()
        {
            if (S == null) { S = new Settings(); S.Load(); }
            toolbar = gameObject.AddComponent<ToolbarControl>();
            toolbar.AddToAllToolbars(() => showWindow = true, () => showWindow = false,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.MAPVIEW,
                MODID, "SPXWebcastOverlayButton", "SPXWebcastOverlay/Icons/f9_38", "SPXWebcastOverlay/Icons/f9_24", MODNAME);
            GameEvents.onHideUI.Add(OnHide); GameEvents.onShowUI.Add(OnShow);
            GameEvents.onVesselChange.Add(OnVesselEvt); GameEvents.onVesselWasModified.Add(OnVesselEvt); GameEvents.onStageActivate.Add(OnStage);
            Instance = this;
            mat = new Material(Shader.Find("Hidden/Internal-Colored"));
            mat.hideFlags = HideFlags.HideAndDontSave;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_Cull", 0); mat.SetInt("_ZWrite", 0);
            mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }
        void OnDestroy()
        {
            GameEvents.onHideUI.Remove(OnHide); GameEvents.onShowUI.Remove(OnShow);
            GameEvents.onVesselChange.Remove(OnVesselEvt); GameEvents.onVesselWasModified.Remove(OnVesselEvt); GameEvents.onStageActivate.Remove(OnStage);
            if (Instance == this) Instance = null;
            if (toolbar != null) { toolbar.OnDestroy(); Destroy(toolbar); }
            if (S != null) S.Save();
        }
        void OnHide() { uiHidden = true; }
        void OnShow() { uiHidden = false; }

        static double UT { get { return Planetarium.GetUniversalTime(); } }

        void Update()
        {
            if (!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel == null) return;
            Vessel v = FlightGlobals.ActiveVessel;
            if (v.id != trackedVessel)
            {
                trackedVessel = v.id;
                if (v.situation == Vessel.Situations.PRELAUNCH) { launched = false; }
                else if (!launched) launched = true; // switching to an already-flying vessel: keep clock as-is
            }
            if (!launched && v.situation != Vessel.Situations.PRELAUNCH)
            {
                launched = true;
                if (double.IsNaN(t0UT) || S.autoZeroOnLiftoff || UT < t0UT || held) { t0UT = UT; held = false; }
            }
        }

        public void StartCountdown() { CountdownFrom(S.countdownStart); }
        public static void CountdownFrom(double n) { held = false; t0UT = UT + Math.Abs(n); }
        public static void HoldClock() { if (!held) { heldT = CurrentMET(); held = true; } }
        public static void ResumeClock() { if (held) { held = false; if (!double.IsNaN(t0UT)) t0UT = UT - heldT; } }
        public static double CurrentMET()
        {
            if (S == null) return 0;
            if (held) return heldT;
            if (double.IsNaN(t0UT)) return -S.countdownStart;
            double t = UT - t0UT;
            if (t > 0 && !launched && S.holdAtZero) t = 0;
            return t;
        }
        public static void ResetStatic() { t0UT = double.NaN; held = false; }
        public void ResetClock()
        {
            t0UT = double.NaN; held = false;
            Vessel v = FlightGlobals.ActiveVessel;
            launched = v != null && v.situation != Vessel.Situations.PRELAUNCH;
        }

        double MissionTime() { return CurrentMET(); }

        // ---------- GUI ----------
        BmpFont fontR, fontB; float hudA; bool fontTried;
        double dSpd, dAlt, dG, dFuel; float dLit;

        void InitStyles(float k)
        {
            sSmall = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Overflow };
            sSmall.normal.textColor = Color.white;
        }

        void LateUpdate()
        {
            bool want = S != null && S.overlayEnabled && HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null && (uiHidden || preview);
            hudA = Mathf.MoveTowards(hudA, want ? 1f : 0f, Time.unscaledDeltaTime / 0.4f);
        }

        void OnGUI()
        {
            if (S.overlayEnabled && hudA > 0.001f && HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null)
                DrawHud(FlightGlobals.ActiveVessel);
            if (showWindow && !uiHidden)
            {
                GUISkin old = GUI.skin; if (HighLogic.Skin != null) GUI.skin = HighLogic.Skin;   // KSP's own window skin
                winRect = ClickThruBlocker.GUILayoutWindow(0x9F9F9, winRect, Window, "SPX Webcast Overlay");
                GUI.skin = old;
            }
        }

        Color A(float a) { return new Color(1, 1, 1, a * hudA); }

        // text sized by CAP HEIGHT in px (measured from the webcast). bold = D-DIN Bold atlas, else D-DIN Exp.
        void Txt(float cx, float cy, string s, float cap, float spacing, float alpha, bool bold = true)
        {
            BmpFont f = bold ? fontB : fontR; if (f == null || !f.Ok) f = bold ? fontR : fontB;
            if (f != null && f.Ok) { f.Draw(s, cx, cy, cap / f.CapRatio, spacing, A(alpha)); return; }
            sSmall.fontSize = Mathf.Max(6, Mathf.RoundToInt(cap / 0.7f)); sSmall.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            GUI.color = A(alpha); GUI.Label(new Rect(cx - 200, cy - 30, 400, 60), s, sSmall); GUI.color = Color.white;
        }
        float TxtW(string s, float cap, float spacing, bool bold = true)
        {
            BmpFont f = bold ? fontB : fontR; if (f == null || !f.Ok) f = bold ? fontR : fontB;
            if (f != null && f.Ok) return f.Measure(s, cap / f.CapRatio, spacing);
            if (sSmall == null) return s.Length * cap * 0.85f;
            sSmall.fontSize = Mathf.Max(6, Mathf.RoundToInt(cap / 0.7f)); return sSmall.CalcSize(new GUIContent(s)).x;
        }

        void DrawHud(Vessel v)
        {
            float k = Screen.height / 1080f;
            if (sSmall == null) InitStyles(k);
            if (!fontTried) { fontTried = true; string fd = Path.Combine(KSPUtil.ApplicationRootPath, "GameData/SPXWebcastOverlay/Fonts/"); fontR = BmpFont.Load(fd + "spx_regular"); fontB = BmpFont.Load(fd + "spx_bold"); }
            float W = Screen.width, H = Screen.height;
            // proportions measured from the reference webcast frame (330 px tall -> scaled to H)
            float gy = H - 104 * k, R = 67 * k, ly = H - 147 * k;
            float xS = 140 * k, xA = 300 * k, xG = W - 300 * k, xE = W - 140 * k;
            double t = MissionTime();

            Part enginePart = null;
            enginePart = FindEnginePart(v);
            int lit = enginePart != null ? ActiveEngines(enginePart) : 0;
            double fuelFrac = enginePart != null ? FuelFraction(v, enginePart) : 0;
            float[] tl = Timeline(t, W);

            if (Event.current.type == EventType.Repaint)
            {
                double e = 1 - Math.Exp(-8.0 * Mathf.Min(Time.unscaledDeltaTime, 0.25f));
                dSpd += (v.srfSpeed * 3.6 - dSpd) * e; dAlt += (v.altitude / 1000.0 - dAlt) * e;
                dG += (v.geeForce - dG) * e; dFuel += (fuelFrac - dFuel) * e; dLit += (lit - dLit) * (float)e;

                mat.SetPass(0);
                GL.PushMatrix(); GL.LoadPixelMatrix(0, W, H, 0);
                BottomGradient(W, H, k);
                ShapedBand(W, H, k);
                Gauge(xS, gy, R, k, dSpd / S.maxSpeedKmh);
                Gauge(xA, gy, R, k, dAlt / S.maxAltKm);
                Gauge(xG, gy, R, k, dG / S.maxG);
                if (enginePart != null) { Gauge(xE, gy, R, k, dFuel); Dots(xE, gy, R, k, lit); }
                DrawTimelineGL(tl, k, ly, t);
                GL.PopMatrix();
            }
            // gauge text: label above, value, unit below
            GaugeText(xS, gy, k, "SPEED", ((int)Math.Round(dSpd)).ToString(), "KM/H");
            GaugeText(xA, gy, k, "ALTITUDE", dAlt.ToString("0.0"), "KM");
            GaugeText(xG, gy, k, "G-FORCE", dG.ToString("0.0"), "G");
            if (enginePart != null) Txt(xE - R - 14 * k, gy + R - 6 * k, lit.ToString(), 10 * k, 0, 0.85f);
            // timeline labels (small, letter-spaced, fading with marker)
            for (int i = 0; i < ev.Count; i++)
            {
                if (float.IsNaN(tl[i]) || ev[i].alpha < 0.01f || ev[i].row == 0) continue;
                Txt(tl[i], ly + ev[i].row * 18 * k, ev[i].Label, 11f * k, 1f * k, 0.85f * ev[i].alpha);
            }
            // clock: small "T+" + large tight digits
            string digits = Clock(t), pre = t < 0 ? "T-" : "T+";
            float dsz = 34.5f * k, psz = 22 * k, dw = TxtW(digits, dsz, 0, false), pw = TxtW(pre, psz, 0), gap = 12 * k;
            float cx0 = W / 2 - (dw + pw + gap) / 2, cy = H - 93 * k;
            Txt(cx0 + pw / 2, cy, pre, psz, 0, 0.7f);
            Txt(cx0 + pw + gap + dw / 2, cy, digits, dsz, 0, 1f, false);
            Txt(W / 2, H - 57 * k, (string.IsNullOrEmpty(S.missionName) ? v.vesselName : S.missionName).Trim().ToUpperInvariant(), 11 * k, 1.5f * k, 0.75f);
        }

        // dark band: tall ends (157 px @1080) behind the gauge pairs, ~40 deg slopes down to a low
        // centre strip (46 px). Plateau 344 px wide, slope run 141 px. Feathered (AA) top edge.
        // Measured from 4K webcast frames (scaled to 1080p): the bottom darkening is a vertical gradient
        // starting ~170 px above the bottom, roughly linear, strongest in the centre under the clock
        // (~85% at the bottom edge) and weaker toward the screen edges (~36%). It never clears in the centre.
        static float GradAlpha(float x, float y, float W, float H, float k)
        {
            float v = Mathf.Clamp01(1f - (H - y) / (170f * k));
            float dx = Mathf.Abs(x - W / 2), u = Mathf.Clamp01((dx - 300f * k) / (600f * k));
            float bell = 1f - u * u * (3 - 2 * u);
            return (0.36f + 0.49f * bell) * v;
        }
        void BottomGradient(float W, float H, float k)
        {
            int nx = 48, ny = 12; float top = H - 175 * k;
            GL.Begin(GL.QUADS);
            for (int j = 0; j < ny; j++)
            {
                float y0 = Mathf.Lerp(top, H, j / (float)ny), y1 = Mathf.Lerp(top, H, (j + 1) / (float)ny);
                for (int i = 0; i < nx; i++)
                {
                    float x0 = W * i / nx, x1 = W * (i + 1) / nx;
                    GL.Color(new Color(0, 0.005f, 0.015f, GradAlpha(x0, y0, W, H, k) * hudA)); GL.Vertex3(x0, y0, 0);
                    GL.Color(new Color(0, 0.005f, 0.015f, GradAlpha(x1, y0, W, H, k) * hudA)); GL.Vertex3(x1, y0, 0);
                    GL.Color(new Color(0, 0.005f, 0.015f, GradAlpha(x1, y1, W, H, k) * hudA)); GL.Vertex3(x1, y1, 0);
                    GL.Color(new Color(0, 0.005f, 0.015f, GradAlpha(x0, y1, W, H, k) * hudA)); GL.Vertex3(x0, y1, 0);
                }
            }
            GL.End();
        }

        // End bands behind the gauge pairs: hard top edge 181 px above the bottom, flat to x=370,
        // then a ~39 deg slope down to (490, H-82) whose contrast fades out; ~38% dark. No centre strip.
        void ShapedBand(float W, float H, float k)
        {
            float yt = H - 181 * k, yb = H - 82 * k, xp = 370 * k, xs = 490 * k, xf = 570 * k;
            float a0 = 0.38f * hudA, a1 = 0.15f * hudA, f = 1.2f * k + 0.5f;
            for (int side = 0; side < 2; side++)
            {
                Func<float, float> X = x => side == 0 ? x : W - x;
                float[] px = { 0, xp, xs, xf }, py = { yt, yt, yb, yb }, pa = { a0, a0, a1, 0 };
                GL.Begin(GL.QUADS);
                for (int i = 0; i < 3; i++)
                {
                    Color ca = new Color(0.02f, 0.025f, 0.035f, pa[i]), cb = new Color(0.02f, 0.025f, 0.035f, pa[i + 1]);
                    GL.Color(ca); GL.Vertex3(X(px[i]), py[i], 0); GL.Color(cb); GL.Vertex3(X(px[i + 1]), py[i + 1], 0);
                    GL.Vertex3(X(px[i + 1]), H, 0); GL.Color(ca); GL.Vertex3(X(px[i]), H, 0);
                    // anti-aliased top edge
                    GL.Color(new Color(ca.r, ca.g, ca.b, 0)); GL.Vertex3(X(px[i]), py[i] - f, 0); GL.Vertex3(X(px[i + 1]), py[i + 1] - f, 0);
                    GL.Color(cb); GL.Vertex3(X(px[i + 1]), py[i + 1], 0); GL.Color(ca); GL.Vertex3(X(px[i]), py[i], 0);
                }
                GL.End();
            }
        }

        void GaugeText(float cx, float cy, float k, string label, string val, string unit)
        {
            Txt(cx, cy - 30 * k, label, 10.5f * k, 1f * k, 0.8f);
            Txt(cx, cy + 1 * k, val, 27 * k, 0, 1f, false);
            Txt(cx, cy + 30 * k, unit, 9.5f * k, 1f * k, 0.75f);
        }

        static string Clock(double t)
        {
            int s = (int)Math.Floor(Math.Abs(t)); if (t < 0) s = (int)Math.Ceiling(-t);
            return string.Format("{0:00}:{1:00}:{2:00}", s / 3600, (s / 60) % 60, s % 60);
        }

        // dynamic window state
        double winA = double.NaN, winB = double.NaN; int lastFrame = -1;
        List<Ev> ev = new List<Ev>();
        void RefreshEvents()
        {
            ev.Clear();
            foreach (var e in S.events) if (e.enabled) ev.Add(e); else { e.alpha = 0; e.row = 0; }
            ev.Sort((a, b) => a.time.CompareTo(b.time));
        }

        double curSpan = double.NaN;

        // returns x positions of 8 markers plus [8]=now-dot x, [9]=line x0, [10]=x1
        float[] Timeline(double t, float W)
        {
            float k = Screen.height / 1080f;
            RefreshEvents();
            float x0 = W / 2 - 460 * k, x1 = W / 2 + 460 * k;
            bool zoomed = t >= -S.zoomTrigger;
            double target = Math.Max(10, zoomed ? S.windowSpan : S.preSpan);
            if (Time.frameCount != lastFrame)
            {
                float dt = lastFrame < 0 ? 1f : Mathf.Min(Time.unscaledDeltaTime, 0.25f);
                lastFrame = Time.frameCount;
                if (double.IsNaN(curSpan)) curSpan = target;
                else curSpan += (target - curSpan) * (1 - Math.Exp(-2.0 * dt)); // ~1.5 s ease
                if (S.staticTimeline) { winA = ev.Count > 0 ? ev[0].time : -10; winB = Math.Max(winA + 1, (ev.Count > 0 ? ev[ev.Count - 1].time : 0) + 30); }
                else { winA = t - curSpan / 2; winB = t + curSpan / 2; }    // now always at centre
                for (int i = 0; i < ev.Count; i++)
                {
                    double et = ev[i].time;
                    double f = (et - winA) / (winB - winA);
                    // fade near the edges so markers glide in on the right and out on the left
                    float edge = Mathf.Clamp01((float)Math.Min(f, 1 - f) / 0.06f);
                    bool want = S.staticTimeline || (f >= 0 && f <= 1);
                    float tgt = want ? (S.staticTimeline ? 1f : edge) : 0f;
                    ev[i].alpha = Mathf.MoveTowards(ev[i].alpha, tgt, dt * 3f);
                    if (!S.staticTimeline) ev[i].alpha = Mathf.Min(ev[i].alpha, edge);
                }
                AssignRows(t, x0, x1);
            }
            int N = ev.Count; float[] r = new float[N + 3];
            for (int i = 0; i < ev.Count; i++)
            {
                double f = (ev[i].time - winA) / (winB - winA);
                r[i] = (f < -0.02 || f > 1.02) ? float.NaN : (float)(x0 + f * (x1 - x0));
            }
            r[N] = (float)(x0 + Math.Max(0, Math.Min(1, (t - winA) / (winB - winA))) * (x1 - x0));
            r[N + 1] = x0; r[N + 2] = x1; return r;
        }

        // place labels below (+1) / above (-1); hide (0) an earlier passed label if both rows collide
        void AssignRows(double t, float x0, float x1)
        {
            float pad = 8 * Screen.height / 1080f;
            float lastBelow = float.NegativeInfinity, lastAbove = float.NegativeInfinity;
            int idxBelow = -1, idxAbove = -1;
            for (int i = 0; i < ev.Count; i++)
            {
                ev[i].row = 0;
                if (ev[i].alpha < 0.01f) continue;
                double f = (ev[i].time - winA) / (winB - winA);
                float x = (float)(x0 + f * (x1 - x0));
                float half = TxtW(ev[i].Label, 11f * Screen.height / 1080f, 1f * Screen.height / 1080f) / 2 + pad;
                if (x - half >= lastBelow) { ev[i].row = 1; lastBelow = x + half; idxBelow = i; }
                else if (x - half >= lastAbove) { ev[i].row = -1; lastAbove = x + half; idxAbove = i; }
                else
                {
                    // both rows taken: drop the earlier passed label from the below row and take its place
                    if (idxBelow >= 0 && t >= ev[idxBelow].time) { ev[idxBelow].row = 0; ev[i].row = 1; lastBelow = x + half; idxBelow = i; }
                    else if (idxAbove >= 0 && t >= ev[idxAbove].time) { ev[idxAbove].row = 0; ev[i].row = -1; lastAbove = x + half; idxAbove = i; }
                }
            }
        }

        void DrawTimelineGL(float[] r, float k, float y, double t)
        {
            int N = r.Length - 3; float x0 = r[N + 1], x1 = r[N + 2], xn = r[N];
            // faint edge-faded line: past bright, future dim
            LineH(x0, xn, y, 1.2f * k, 0.85f, k); LineH(xn, x1, y, 1f * k, 0.28f, k);
            for (int i = 0; i < ev.Count; i++)
            {
                if (float.IsNaN(r[i]) || ev[i].alpha < 0.01f) continue;
                bool passed = t >= ev[i].time; float a = ev[i].alpha;
                Rectf(r[i] - 0.5f * k, y - 7 * k, r[i] + 0.5f * k, y - 4.5f * k, A(0.35f * a)); // faint tick
                if (passed) Disc(r[i], y, 3.6f * k, A(a));
                else { Disc(r[i], y, 3.6f * k, new Color(0, 0, 0, 0.6f * a * hudA)); Ring(r[i], y, 3.6f * k, 1.1f * k, A(a)); }
            }
            Glow(xn, y, 5f * k, 9f * k, A(0.35f));
            Disc(xn, y, 5f * k, A(1));
        }
        void LineH(float xa, float xb, float y, float th, float a, float k)
        {
            if (xb <= xa) return;
            GL.Begin(GL.QUADS);
            float f = k; // 1px feather
            Quad(xa, y - th / 2 - f, xb, y - th / 2, A(0), A(0), A(a), A(a));
            Quad(xa, y - th / 2, xb, y + th / 2, A(a), A(a), A(a), A(a));
            Quad(xa, y + th / 2, xb, y + th / 2 + f, A(a), A(a), A(0), A(0));
            GL.End();
        }
        static void Quad(float x0, float y0, float x1, float y1, Color tl, Color tr, Color br, Color bl)
        {
            GL.Color(tl); GL.Vertex3(x0, y0, 0); GL.Color(tr); GL.Vertex3(x1, y0, 0);
            GL.Color(br); GL.Vertex3(x1, y1, 0); GL.Color(bl); GL.Vertex3(x0, y1, 0);
        }
        static void Rectf(float x0, float y0, float x1, float y1, Color c)
        {
            GL.Begin(GL.QUADS); GL.Color(c);
            GL.Vertex3(x0, y0, 0); GL.Vertex3(x1, y0, 0); GL.Vertex3(x1, y1, 0); GL.Vertex3(x0, y1, 0); GL.End();
        }
        // radial band between r0 and r1 with colours c0 (at r0) and c1 (at r1); angle 0 = up, clockwise
        static void Band(float cx, float cy, float r0, float r1, float a0, float a1, Color c0, Color c1)
        {
            if (r0 < 0) r0 = 0;
            int n = Mathf.Clamp((int)(Mathf.Abs(a1 - a0) * Mathf.Max(r1, 4) / 3f), 8, 256);
            GL.Begin(GL.QUADS);
            for (int i = 0; i < n; i++)
            {
                float u = Mathf.Lerp(a0, a1, i / (float)n), v = Mathf.Lerp(a0, a1, (i + 1) / (float)n);
                float su = Mathf.Sin(u), cu = Mathf.Cos(u), sv = Mathf.Sin(v), cv = Mathf.Cos(v);
                GL.Color(c1); GL.Vertex3(cx + su * r1, cy - cu * r1, 0); GL.Vertex3(cx + sv * r1, cy - cv * r1, 0);
                GL.Color(c0); GL.Vertex3(cx + sv * r0, cy - cv * r0, 0); GL.Vertex3(cx + su * r0, cy - cu * r0, 0);
            }
            GL.End();
        }
        // anti-aliased arc: stroke from r-w to r with 1px feathered edges
        static void Arc(float cx, float cy, float r, float w, float a0, float a1, Color c)
        {
            float f = Mathf.Max(0.8f, Screen.height / 1080f); Color z = new Color(c.r, c.g, c.b, 0);
            Band(cx, cy, r - w - f, r - w, a0, a1, z, c);
            Band(cx, cy, r - w, r, a0, a1, c, c);
            Band(cx, cy, r, r + f, a0, a1, c, z);
        }
        static void Ring(float cx, float cy, float r, float w, Color c) { Arc(cx, cy, r, w, 0, Mathf.PI * 2, c); }
        static void Disc(float cx, float cy, float r, Color c) { Arc(cx, cy, r, r, 0, Mathf.PI * 2, c); }
        static void Glow(float cx, float cy, float r, float g, Color c)
        { Band(cx, cy, r, r + g, 0, Mathf.PI * 2, c, new Color(c.r, c.g, c.b, 0)); }

        void Gauge(float cx, float cy, float R, float k, double frac)
        {
            float f = Mathf.Clamp01((float)frac);
            float a0 = -0.75f * Mathf.PI, span = 1.5f * Mathf.PI;
            // measured (4K webcast, /2): 4 px track at outer radius 67, ~25% white; thin inner ring at r=57
            Disc(cx, cy, R + 5 * k, new Color(0, 0, 0, 0.35f * hudA));               // subtle dark disc
            Arc(cx, cy, R, 4f * k, a0, a0 + span, A(0.25f));                         // dim track
            Ring(cx, cy, R * 0.85f, 1f * k, A(0.25f));                                // thin inner ring
            if (f > 0.002f) Arc(cx, cy, R, 4f * k, a0, a0 + span * f, A(1f));         // bright value
        }
        void Dots(float cx, float cy, float R, float k, int lit)
        {
            // measured from the webcast: 8 outer dots on a ring of radius 34.3 px (1080p) at 22.5 + 45*i deg
            // (no dot at the top), centre dot, dot radius 10.25 px. Lit = solid white, unlit = ~27% white fill.
            // 3-engine burn: centre + outer dots at 67.5 deg (upper right) and 247.5 deg (lower left).
            float rr = 34.3f * k * (R / (67f * k)), dr = 10.25f * k * (R / (67f * k));
            for (int i = -1; i < 8; i++)
            {
                float a = (22.5f + 45f * i) * Mathf.Deg2Rad;
                float x = i < 0 ? cx : cx + Mathf.Sin(a) * rr, y = i < 0 ? cy : cy - Mathf.Cos(a) * rr;
                bool on = i < 0 ? lit >= 1 : (lit >= 9 || (lit == 3 && (i == 1 || i == 5)));
                Disc(x, y, dr, on ? A(1f) : A(0.27f));
            }
        }

        // ---- engine part detection ----
        // KSP stores AvailablePart.name with '_' replaced by '.', so the cfg name TE_19_F9_S1_Engine is
        // "TE.19.F9.S1.Engine" at runtime. Compare normalised names, with a module-signature fallback.
        Part cachedEngine; Guid cachedVessel; bool rescan = true; float lastScan; string lastScanMsg;
        void OnVesselEvt(Vessel v) { rescan = true; }
        void OnStage(int st) { rescan = true; }
        static string Norm(string n) { return (n ?? "").Replace('.', '_').Replace(' ', '_').ToUpperInvariant(); }
        static bool HasTundraOctawebEngines(Part p)
        {
            bool all = false, three = false, one = false;
            foreach (PartModule pm in p.Modules)
            {
                var fx = pm as ModuleEnginesFX; if (fx == null) continue;
                if (fx.engineID == "AllEngines") all = true; else if (fx.engineID == "ThreeLanding") three = true; else if (fx.engineID == "CenterOnly") one = true;
            }
            return all && three && one;
        }
        Part FindEnginePart(Vessel v)
        {
            if (v == null) return null;
            bool stale = cachedEngine != null && (cachedEngine.vessel != v || cachedEngine.State == PartStates.DEAD);
            if (!rescan && !stale && v.id == cachedVessel && (cachedEngine != null || Time.unscaledTime - lastScan < 2f)) return cachedEngine;
            rescan = false; lastScan = Time.unscaledTime; cachedVessel = v.id; cachedEngine = null;
            string how = null;
            foreach (Part p in v.parts)
                if ((p.partInfo != null && Norm(p.partInfo.name) == "TE_19_F9_S1_ENGINE") || Norm(p.name) == "TE_19_F9_S1_ENGINE") { cachedEngine = p; how = "name"; break; }
            if (cachedEngine == null)
                foreach (Part p in v.parts) if (HasTundraOctawebEngines(p)) { cachedEngine = p; how = "engineIDs"; break; }
            string msg = cachedEngine != null
                ? "[SPXWebcastOverlay] engines gauge: found '" + (cachedEngine.partInfo != null ? cachedEngine.partInfo.name : cachedEngine.name) + "' by " + how + " on " + v.vesselName
                : "[SPXWebcastOverlay] engines gauge: no TE_19_F9_S1_Engine (or AllEngines/ThreeLanding/CenterOnly part) on " + v.vesselName + " (" + v.parts.Count + " parts)";
            if (msg != lastScanMsg) { Debug.Log(msg); lastScanMsg = msg; }
            return cachedEngine;
        }

        static int ActiveEngines(Part p)
        {
            int best = 0;
            foreach (PartModule pm in p.Modules)
            {
                ModuleEngines e = pm as ModuleEngines;
                if (e == null || !e.EngineIgnited || e.flameout || e.currentThrottle <= 0) continue;
                if (!e.isEnabled && e.finalThrust <= 0.001f) continue;   // switched-off mode of ModuleTundraEngineSwitch
                string id = (e is ModuleEnginesFX) ? ((ModuleEnginesFX)e).engineID : "";
                int n = id == "AllEngines" ? 9 : id == "ThreeLanding" ? 3 : id == "CenterOnly" ? 1 : 0;
                if (n > best) best = n;
            }
            return best;
        }

        double FuelFraction(Vessel v, Part engine)
        {
            IEnumerable<Part> parts = v.parts;
            try
            {
                if (engine.crossfeedPartSet != null)
                {
                    var set = engine.crossfeedPartSet.GetParts();
                    if (set != null && set.Count > 0) parts = set;
                }
            }
            catch { }
            double amt = 0, max = 0;
            foreach (Part p in parts)
                foreach (PartResource r in p.Resources)
                    if (r.resourceName == "LiquidFuel" || r.resourceName == "Oxidizer") { amt += r.amount; max += r.maxAmount; }
            if (max <= 0) return 0;
            return amt / max;
        }

        // ---------- settings window ----------
        void Window(int id)
        {
            var ob = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold, fixedHeight = 36 };
            ob.normal.textColor = ob.hover.textColor = S.overlayEnabled ? Color.green : new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("Overlay enabled: " + (S.overlayEnabled ? "ON" : "OFF"), ob)) { S.overlayEnabled = !S.overlayEnabled; S.Save(); }
            GUILayout.Space(6);
            GUILayout.BeginHorizontal(); GUILayout.Label("Mission name", GUILayout.Width(110));
            S.missionName = GUILayout.TextField(S.missionName ?? "", 40); GUILayout.EndHorizontal();
            GUILayout.Label("(empty = vessel name; shown uppercase under the clock)");
            GUILayout.Label("Countdown / clock");
            S.countdownStart = Field("Countdown start (s before T-0)", "cd", S.countdownStart);
            GUILayout.Label("Clock: " + (MissionTime() < 0 ? "T- " : "T+ ") + Clock(MissionTime()) + (launched ? "  (launched)" : ""));
            GUILayout.BeginHorizontal();
            GUI.enabled = HighLogic.LoadedSceneIsFlight;
            if (GUILayout.Button("Start Countdown")) StartCountdown();
            if (GUILayout.Button("Reset")) ResetClock();
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            S.autoZeroOnLiftoff = GUILayout.Toggle(S.autoZeroOnLiftoff, "Auto-zero clock on liftoff");
            S.holdAtZero = GUILayout.Toggle(S.holdAtZero, "Hold at T-0 until liftoff");
            GUILayout.Space(6); GUILayout.Label("Timeline events (on / name / T+ seconds)");
            evScroll = GUILayout.BeginScrollView(evScroll, GUILayout.Height(Mathf.Min(260, 26 * S.events.Count + 8)));
            Ev remove = null;
            foreach (var e in S.events)
            {
                GUILayout.BeginHorizontal();
                e.enabled = GUILayout.Toggle(e.enabled, "", GUILayout.Width(20));
                if (e.builtin) GUILayout.Label(e.Label, GUILayout.Width(170));
                else e.name = GUILayout.TextField(e.name ?? "", 32, GUILayout.Width(170));
                e.time = TimeField("ev" + e.id, e.time);
                if (!e.builtin) { if (GUILayout.Button("X", GUILayout.Width(24))) remove = e; }
                else GUILayout.Space(28);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            if (remove != null) { S.events.Remove(remove); edit.Remove("ev" + remove.id); }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add event")) S.events.Add(new Ev { name = "NEW EVENT", time = Math.Round(Math.Max(0, MissionTime())) + 30, builtin = false });
            if (GUILayout.Button("Reset events to defaults")) { foreach (var e in S.events) edit.Remove("ev" + e.id); S.events = Settings.DefaultEvents(); }
            GUILayout.EndHorizontal();
            GUILayout.Label("(LIFTOFF can be hidden; the clock still zeroes at liftoff)");
            S.staticTimeline = GUILayout.Toggle(S.staticTimeline, "Static full timeline (first event .. last event + 30 s)");
            S.zoomTrigger = Field("Zoom to liftoff at T- (s)", "zt", S.zoomTrigger);
            S.preSpan = Field("Pre-zoom window span (s)", "ps", S.preSpan);
            S.windowSpan = Field("Zoomed / flight window span (s)", "ws", S.windowSpan);
            GUILayout.Space(6); GUILayout.Label("Gauge maxima");
            S.maxSpeedKmh = Field("Speed max (km/h)", "ms", S.maxSpeedKmh);
            S.maxAltKm = Field("Altitude max (km)", "ma", S.maxAltKm);
            S.maxG = Field("G-force max", "mg", S.maxG);
            GUILayout.Space(6);
            preview = GUILayout.Toggle(preview, "Preview HUD (show with UI visible)");
            S.perSave = GUILayout.Toggle(S.perSave, "Also save settings per save game");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save")) S.Save();
            if (GUILayout.Button("Defaults")) { var p = S.perSave; S = new Settings(); S.perSave = p; edit.Clear(); }
            if (GUILayout.Button("Close")) { showWindow = false; toolbar.SetFalse(false); S.Save(); }
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }
        Vector2 evScroll;
        double TimeField(string key, double val)
        {
            string s; if (!edit.TryGetValue(key, out s)) s = val.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            s = GUILayout.TextField(s, GUILayout.Width(70)); edit[key] = s;
            double d; return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d) ? d : val;
        }
        double Field(string label, string key, double val)
        {
            string s; if (!edit.TryGetValue(key, out s)) s = val.ToString("0.###");
            GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(220));
            s = GUILayout.TextField(s, GUILayout.Width(100)); GUILayout.EndHorizontal();
            edit[key] = s;
            double d; return double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d) ? d : val;
        }
    }

    // Bitmap font atlas renderer (atlas pre-rendered with PIL from Barlow SemiBold, OFL)
    public class BmpFont
    {
        public bool Ok; Texture2D tex; float baseSize, asc, desc, cap;
        public float CapRatio { get { return cap / baseSize; } }
        struct G { public float x, y, w, h, xo, adv; }
        Dictionary<char, G> g = new Dictionary<char, G>();
        public static BmpFont Load(string basePath)
        {
            var f = new BmpFont();
            try
            {
                if (!File.Exists(basePath + ".png") || !File.Exists(basePath + ".txt")) return f;
                f.tex = new Texture2D(2, 2, TextureFormat.ARGB32, true);
                if (!ImageConversion.LoadImage(f.tex, File.ReadAllBytes(basePath + ".png"))) return f;
                f.tex.filterMode = FilterMode.Trilinear; f.tex.anisoLevel = 4; f.tex.wrapMode = TextureWrapMode.Clamp;
                f.tex.mipMapBias = -0.5f;
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                string[] lines = File.ReadAllLines(basePath + ".txt");
                string[] h = lines[0].Split(' ');
                f.baseSize = float.Parse(h[1], ci); f.asc = float.Parse(h[3], ci); f.desc = float.Parse(h[5], ci);
                f.cap = (h.Length > 7 && h[6] == "cap") ? float.Parse(h[7], ci) : f.baseSize * 0.7f;
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] p = lines[i].Split(' '); if (p.Length < 7) continue;
                    f.g[(char)int.Parse(p[0])] = new G { x = float.Parse(p[1], ci), y = float.Parse(p[2], ci), w = float.Parse(p[3], ci), h = float.Parse(p[4], ci), xo = float.Parse(p[5], ci), adv = float.Parse(p[6], ci) };
                }
                f.Ok = f.g.Count > 0;
            }
            catch (Exception e) { Debug.LogWarning("[SPXWebcastOverlay] font load failed, using default: " + e.Message); f.Ok = false; }
            return f;
        }
        public float Measure(string s, float size, float spacing)
        {
            float sc = size / baseSize, w = 0; G q;
            for (int i = 0; i < s.Length; i++) w += (g.TryGetValue(s[i], out q) ? q.adv * sc : size * 0.5f) + (i < s.Length - 1 ? spacing : 0);
            return w;
        }
        // draws centred on (cx, cy) where cy is the vertical centre of capital letters
        public void Draw(string s, float cx, float cy, float size, float spacing, Color c)
        {
            if (Event.current.type != EventType.Repaint) return;
            float sc = size / baseSize, x = cx - Measure(s, size, spacing) / 2;
            float capH = cap;
            float top = cy - (asc - capH / 2) * sc;
            Color old = GUI.color; GUI.color = c; G q;
            foreach (char ch in s)
            {
                if (g.TryGetValue(ch, out q))
                {
                    if (ch != ' ')
                    {
                        var rect = new Rect(x + q.xo * sc, top, q.w * sc, q.h * sc);
                        var uv = new Rect(q.x / tex.width, 1f - (q.y + q.h) / tex.height, q.w / tex.width, q.h / tex.height);
                        GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
                    }
                    x += q.adv * sc + spacing;
                }
                else x += size * 0.5f + spacing;
            }
            GUI.color = old;
        }
    }

    /// <summary>Public API used by the optional kOS bridge (kOS.WCOL.dll). No kOS dependency here.</summary>
    public static class WebcastAPI
    {
        static Settings S { get { if (OverlayAddon.S == null) { OverlayAddon.S = new Settings(); OverlayAddon.S.Load(); } return OverlayAddon.S; } }
        public static bool Available { get { return true; } }
        public static void CountdownStart() { OverlayAddon.CountdownFrom(S.countdownStart); }
        public static void CountdownFrom(double n) { var x = S; OverlayAddon.CountdownFrom(n); }
        public static void Hold() { OverlayAddon.HoldClock(); }
        public static void Resume() { OverlayAddon.ResumeClock(); }
        public static void Reset() { if (OverlayAddon.Instance != null) OverlayAddon.Instance.ResetClock(); else OverlayAddon.ResetStatic(); }
        public static void SetMission(string name) { S.missionName = name ?? ""; S.Save(); }
        public static void Show() { S.overlayEnabled = true; S.Save(); }
        public static void Hide() { S.overlayEnabled = false; S.Save(); }
        public static double MET { get { var x = S; return OverlayAddon.CurrentMET(); } }
    }
}
