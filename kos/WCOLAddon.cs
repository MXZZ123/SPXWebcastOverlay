using System.Reflection;
using kOS;
using kOS.AddOns;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using SPXWebcastOverlay;

[assembly: AssemblyTitle("kOS.WCOL")]
[assembly: AssemblyDescription("kOS bridge for SPXWebcastOverlay: ADDONS:WCOL")]
[assembly: KSPAssembly("kOS.WCOL", 1, 0, 0)]
// kOS only scans assemblies named kOS.* or depending on "kOS"; we also need the overlay loaded first.
[assembly: KSPAssemblyDependency("kOS", 1, 3)]
[assembly: KSPAssemblyDependency("SPXWebcastOverlay", 1, 0)]

namespace WCOL
{
    [kOSAddon("WCOL")]
    [KOSNomenclature("WCOLAddon")]
    public class WCOLAddon : kOS.Suffixed.Addon
    {
        public WCOLAddon(SharedObjects shared) : base(shared) { InitializeSuffixes(); }

        private void InitializeSuffixes()
        {
            AddSuffix("COUNTDOWNSTART", new NoArgsVoidSuffix(() => WebcastAPI.CountdownStart(), "Start the webcast countdown from the configured start (T-0 = now + countdown start)"));
            AddSuffix("COUNTDOWNFROM", new OneArgsSuffix<ScalarValue>(n => WebcastAPI.CountdownFrom(n.GetDoubleValue()), "Start the countdown from T-n seconds"));
            AddSuffix("HOLD", new NoArgsVoidSuffix(() => WebcastAPI.Hold(), "Hold (freeze) the webcast clock"));
            AddSuffix("RESUME", new NoArgsVoidSuffix(() => WebcastAPI.Resume(), "Resume a held clock"));
            AddSuffix("RESET", new NoArgsVoidSuffix(() => WebcastAPI.Reset(), "Reset the clock"));
            AddSuffix("SETMISSION", new OneArgsSuffix<StringValue>(s => WebcastAPI.SetMission(s.ToString()), "Set the mission name shown under the clock"));
            AddSuffix("SHOW", new NoArgsVoidSuffix(() => WebcastAPI.Show(), "Enable the overlay"));
            AddSuffix("HIDE", new NoArgsVoidSuffix(() => WebcastAPI.Hide(), "Disable the overlay"));
            AddSuffix("MET", new Suffix<ScalarValue>(() => WebcastAPI.MET, "Seconds relative to T-0 (negative before)"));
            // AVAILABLE is registered by the kOS base class and calls Available().
        }

        public override BooleanValue Available() { return WebcastAPI.Available; }
    }
}
