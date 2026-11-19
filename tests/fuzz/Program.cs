// PZA's UI Probe for Meitou: the fuzz harness. Author: PZA. Licence: GPLv3 or later. Never shipped; it never starts the game.
// The probe is a test mod for PZA's Meitou UI, so its harness is the library's own targets on the probe's settings and strings: the kept
// settings file, language files, the character wrap, the font reader and the real fonts (PzaFuzzUi).
using System.Reflection;
using PZA.MeitouUi.Fuzz;
using PZA.UiProbe.Meitou;

static class Program
{
    static (string, string)[] Table() =>
        [.. ((Dictionary<string, string>)typeof(PZA.MeitouUi.PzaStrings).GetField("_en", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(Probe.Strings)!)
            .Select(kv => (kv.Key, kv.Value))];

    static int Main(string[] a) => PzaFuzz.Run("PZA's UI Probe for Meitou", a,
        [.. PzaFuzzUi.Settings("pza.uiprobe", () => new ProbeSettings()), PzaFuzzUi.Strings(Table()), .. PzaFuzzUi.Library(1000)]);
}
