// PZA's UI Probe for Meitou. Author: PZA. Licence: GPLv3 or later.
// The probe window and tiny button, written the way every PZA Meitou mod will write its windows (PZA's Meitou UI): PzaLayout for the layout,
// PzaWindow's helpers in Bind, one Event handler, the place in the mod's settings, PzaTinyButton for the small form.
using System.Globalization;
using System.Numerics;
using Meitou.Game.Ui;
using Meitou.Modding;
using PZA.MeitouUi;
using L = PZA.MeitouUi.PzaLayout;

namespace PZA.UiProbe.Meitou;

/// <summary>The probe's settings: where its window and tiny button sit (fractions of the screen), the tiny button's opacity. Written by the
/// library when the player moves them; shown on Settings > Mods > PZAUiProbe.</summary>
public sealed class ProbeSettings
{
    [Setting("Window place across", Hint = "Where the window was left (a fraction of the screen from its layout position). Drag the window to change it.", Min = -1, Max = 1, Step = 0.001)]
    public double WindowX { get; set; }
    [Setting("Window place down", Hint = "As above, down.", Min = -1, Max = 1, Step = 0.001)]
    public double WindowY { get; set; }
    [Setting("Tiny button across", Hint = "Drag the tiny button to change it.", Min = 0, Max = 1, Step = 0.001)]
    public double TinyX { get; set; } = 1;
    [Setting("Tiny button down", Hint = "Drag the tiny button to change it.", Min = 0, Max = 1, Step = 0.001)]
    public double TinyY { get; set; } = 0.12;
    [Setting("Tiny button opacity", Hint = "The mouse wheel over the tiny button changes it.", Min = 0.35, Max = 1, Step = 0.05)]
    public double TinyOpacity { get; set; } = 1;
}

public static class Probe
{
    public const string Id = "pza.uiprobe";
    public static ProbeSettings Settings = new();
    public static ModSettingsGroup? Group;
    static void Set(string key, double v) => PzaSettings.Set(Group, Settings, key, v);
    public static PzaWindow Window = null!;
    public static PzaTinyButton Tiny = null!;

    public static readonly PzaStrings Strings = new([
        ("title", "PZA'S UI PROBE"),
        ("tab.widgets", "WIDGETS"),
        ("tab.faces", "FACES"),
        ("tab.help", "HELP"),
        ("intro", "A real Kenshi window from a code mod, on PZA's Meitou UI. Drag it by its title, press Escape to close it, type in the box, pick a line, move the slider. It reopens where you left it, also after a restart."),
        ("tick.on", "Tick box: ON"),
        ("tick.off", "Tick box: off"),
        ("slider", "Slider: {1}x (0.10 - 0.60, 0.05 steps)"),
        ("edit.empty", "Edit box: type here"),
        ("edit.some", "Edit box: \"{1}\" ({2} letters)"),
        ("help", "Help as a tab of the same window: the PZA UI rules' help page, wrapped by the game. A second paragraph checks that long text wraps inside the window and does not run past its frame at any resolution."),
        ("faces.none", "Select up to four characters (yours or anyone's): their own faces show here, drawn live by Meitou's portrait renderer."),
        ("faces.some", "{1} selected: the first four faces below. A recruit must show their own face, not a generic one."),
        ("faces.off", "No faces: this Meitou lacks the portrait members (see the log)."),
        ("status.none", "Not pressed yet"),
        ("status.some", "Pressed {1} time(s)"),
        ("press", "PRESS"),
        ("press.tip", "A native Kenshi tooltip, set through PZA's Meitou UI."),
        ("close", "CLOSE"),
        ("tiny", "UI PROBE"),
    ]);

    // the window's state
    public static bool Tick;
    public static int SliderPos = 8;
    public static string Edited = "";
    public static int Selected = -1;
    public static int Presses;
    static readonly string[] s_items = ["Dust bandit", "Hungry bandit", "Holy Nation outlaw", "Shek warrior", "Hive soldier drone", "Skeleton"];
    static int s_eventLogs;

    /// <summary>Whose faces the Faces tab shows: the player's selection (the offline check sets its own).</summary>
    public static Func<IReadOnlyList<global::Meitou.Simulation.CharacterId>> FacesFor = PzaUi.SelectedCharacters;

    public static string Speed(int pos) => (0.10 + 0.05 * pos).ToString("0.00", CultureInfo.InvariantCulture);

    public static string LayoutXml() => L.Doc(L.WindowMin("Root", "PZA'S UI PROBE", 0.32, 0.18, 0.36, 0.62,
        L.Tabs("Tabs", 0.03, 0.02, 0.94, 0.84,
            ("TabWidgets", [
                L.Text("lbIntro", 0.03, 0.03, 0.94, 0.12, wrap: true),
                L.Tick("Tick", 0.03, 0.17, 0.06, 0.86),
                L.Text("lbSlider", 0.03, 0.26, 0.94, 0.06),
                L.Slider("Slider", 0.03, 0.33, 0.94, 0.07, range: 11, pos: 8),
                L.Text("lbEdit", 0.03, 0.43, 0.94, 0.06),
                L.Edit("Edit", 0.03, 0.50, 0.94, 0.07, maxLength: 40),
                L.List("List", 0.03, 0.60, 0.94, 0.37)]),
            ("TabFaces", [
                L.Text("lbFaces", 0.03, 0.03, 0.94, 0.12, wrap: true),
                L.Portrait("Face0", 0.03, 0.18, 0.22, 0.293),
                L.Portrait("Face1", 0.27, 0.18, 0.22, 0.293),
                L.Portrait("Face2", 0.51, 0.18, 0.22, 0.293),
                L.Portrait("Face3", 0.75, 0.18, 0.22, 0.293)]),
            L.HelpPage("TabHelp", "lbHelp")),
        L.Text("lbStatus", 0.03, 0.875, 0.5, 0.05),
        L.Button("PressButton", 0.53, 0.87, 0.21, 0.07),
        L.Button("CloseButton", 0.76, 0.87, 0.21, 0.07, isDefault: true)));

    public static void Setup()
    {
        var S = Strings;
        Window = new PzaWindow(Id, LayoutXml)
        {
            Minimizable = true,   // the "-" beside close, or a double click on the title: the tiny button takes over
            Minimized = () => Counters.Add("minimized"),
            Bind = b =>
            {
                PzaWindow.Enable(b, "Tick", "PressButton", "CloseButton");
                b.Text["Root"] = S.T("title");
                PzaWindow.TabText(b, "Tabs", 0, S.T("tab.widgets"));
                PzaWindow.TabText(b, "Tabs", 1, S.T("tab.faces"));
                PzaWindow.TabText(b, "Tabs", 2, S.T("tab.help"));
                var faces = FacesFor();
                b.Text["lbFaces"] = !PzaPortrait.Available ? S.T("faces.off") : faces.Count == 0 ? S.T("faces.none") : S.TF("faces.some", faces.Count.ToString(CultureInfo.InvariantCulture));
                for (int i = 0; i < 4; i++)
                {
                    if (i < faces.Count) PzaPortrait.Face(b, "Face" + i.ToString(CultureInfo.InvariantCulture), faces[i]);
                    else PzaPortrait.Clear(b, "Face" + i.ToString(CultureInfo.InvariantCulture));
                }
                b.Text["lbIntro"] = S.T("intro");
                b.Text["lbTick"] = S.T(Tick ? "tick.on" : "tick.off");
                PzaWindow.Tick(b, "Tick", Tick);
                b.Text["lbSlider"] = S.TF("slider", Speed(SliderPos));
                b.Text["lbEdit"] = Edited.Length == 0 ? S.T("edit.empty") : S.TF("edit.some", Edited, Edited.Length.ToString(CultureInfo.InvariantCulture));
                PzaWindow.Lines(b, "List", s_items, Selected);
                b.Text["lbHelp"] = S.T("help");
                b.Text["lbStatus"] = Presses == 0 ? S.T("status.none") : S.TF("status.some", Presses.ToString(CultureInfo.InvariantCulture));
                b.Text["PressButton"] = S.T("press");
                PzaWindow.Tooltip(b, "PressButton", S.T("press.tip"));
                b.Text["CloseButton"] = S.T("close");
            },
            Event = OnEvent,
            LoadPlace = () => new Vector2((float)Settings.WindowX, (float)Settings.WindowY),
            SavePlace = p =>
            {
                Set(nameof(ProbeSettings.WindowX), Math.Round(p.X, 3)); Set(nameof(ProbeSettings.WindowY), Math.Round(p.Y, 3));
                Counters.Add("place.saved");
                Log.Line($"window place saved: {Settings.WindowX:0.###}, {Settings.WindowY:0.###} of the screen");
            },
            OpenChanged = open => Counters.Add(open ? "opened" : "closed"),
        };
        PzaUi.Add(Window);

        Tiny = new PzaTinyButton("probe")
        {
            Caption = () => S.T("tiny"),
            Visible = () => !Window.IsOpen,   // the PZA pattern: the full window or its tiny button
            Click = () => { Window.Toggle(); Counters.Add("tiny.click"); },
            OpenFull = () => { Window.Open(); Counters.Add("tiny.plus"); },
            LoadPlace = () => new Vector2((float)Settings.TinyX, (float)Settings.TinyY),
            SavePlace = p => { Set(nameof(ProbeSettings.TinyX), Math.Round(p.X, 3)); Set(nameof(ProbeSettings.TinyY), Math.Round(p.Y, 3)); Counters.Add("tiny.moved"); },
            LoadOpacity = () => Settings.TinyOpacity,
            SaveOpacity = a => { Set(nameof(ProbeSettings.TinyOpacity), a); Counters.Add("tiny.opacity"); },
        };
        PzaUi.Add(Tiny);
    }

    public static void OnEvent(MyGuiEvent e)
    {
        Counters.Add("event." + e.Action);
        if (s_eventLogs++ < 60) Log.Line($"event: widget '{e.Widget}' action '{e.Action}' text '{e.Text}' value {e.Value.ToString("0.##", CultureInfo.InvariantCulture)}");
        switch (e.Action)
        {
            case "Tick": Tick = !Tick; break;
            case "PressButton": Presses++; break;
            case "CloseButton": Window.Close(); break;
            case "scroll": SliderPos = Math.Clamp((int)Math.Round(e.Value), 0, 10); break;
            case "text": Edited = e.Text; break;
            case "select" or "accept": Selected = (int)e.Value; break;
        }
    }
}
