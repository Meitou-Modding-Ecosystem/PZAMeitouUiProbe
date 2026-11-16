// PZA's UI Probe for Meitou. Author: PZA. Licence: GPLv3 or later.
// 0.3.0: a Faces tab (PZA's Meitou UI 0.2.0's character faces: the selected characters' own faces).
// 0.2.0: the probe rebuilt on PZA's Meitou UI (the shared window library, ..\..\PZAMeitouUi\src, compiled in), so one in-game test proves the
// library: its window (layout builder, bind helpers, events, the place remembered across sessions), its tiny HUD button and its string table.
// Opened from F10 > "PZA's UI Probe: open / close", or the tiny "UI PROBE" button (top right; drag it, wheel = opacity, + opens the window).
using System.Numerics;
using System.Reflection;
using Meitou.Localisation;
using Meitou.Modding;
using PZA.MeitouUi;

namespace PZA.UiProbe.Meitou;

public sealed class ProbeMod : IMod
{
    public void Initialize(ModContext context)
    {
        Log.Sink = context.Log;
        PzaUi.Log = s => Log.Line(s);
        PzaUi.EachFrame = Log.Tick;
        Probe.Group = context.AddSettings(Probe.Settings);
        string version = typeof(ProbeMod).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        int plus = version.IndexOf('+');
        if (plus >= 0) version = version[..plus];
        Log.Line($"{version} (private test mod, by PZA) on PZA's Meitou UI {PzaUi.Version}: F10 > 'PZA's UI Probe: open / close', or the tiny UI PROBE button");
        foreach (var line in Probe.Strings.Init(context.Folder, Loc.Language?.Code ?? "en", "auto")) Log.Line(line);

        Probe.Setup();
        if (!PzaUi.Install(context)) return;
        context.AddDebugCommand("PZA's UI Probe: open / close", _ => { Probe.Window.Toggle(); return "UI probe: toggling"; });
    }
}
