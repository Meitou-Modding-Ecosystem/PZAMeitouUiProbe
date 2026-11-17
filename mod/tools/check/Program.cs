// PZA's UI Probe for Meitou 0.3.0: offline check of the probe AND of PZA's Meitou UI (the library compiled into it). Author PZA, GPLv3 or later.
// Never shipped; it does not start the game; it reads the Kenshi install.
// 1. Meitou 0.20.1's manifest rules; the settings page accepts the probe's settings.
// 2. PzaUi.Install against the real MeitouClient.dll: every member resolves, every patch applies (the window patches and the pointer calls).
// 3. PzaWindow on a REAL MyGuiScreenHost with Kenshi's own MyGUI resources: register on the first frame, open / close / toggle, the bind (after the
//    game's default that disables unknown buttons) through the library's helpers, each widget's event, the tooltip property, real tab pages, a
//    title-bar drag saved as a fraction of the screen, the place restored on a NEW host (a restart) at another resolution, Escape.
// 4. PzaTinyButton through Meitou's real UiHost: laid out in the top-right corner, a click (press + release, no drag) toggles the window, a drag
//    moves it and saves the place without toggling, the wheel steps its opacity to the 0.35 floor and is ours, the + opens the window.
// 5. PzaPortrait: the GatherPortraitKeys patch applies to the real GameHost; the Faces tab binds the atlas cells; the requests: ours added once
//    per frame, moved off a cell another request takes, dropped when no longer bound. (The live drawing needs the game: in-game test.)
// 6. PzaStrings: a translation file with a lost placeholder and a bad escape is refused line by line.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Meitou.Content;
using Meitou.Data.Ui;
using Meitou.Game.Host.Input;
using Meitou.Game.Rendering.Characters;
using Meitou.Game.Ui;
using Meitou.Simulation;
using PZA.MeitouUi;
using PZA.UiProbe.Meitou;

static class P
{
    static int s_bad;

    static int Main(string[] a)
    {
        string dir = a[0], kenshi = a.Length > 1 ? a[1] : @"C:\Program Files (x86)\Steam\steamapps\common\Kenshi";
        var lines = new List<string>();
        PzaUi.Log = lines.Add;
        Log.Sink = lines.Add;
        Manifest(dir);
        Probe.Setup();
        Expect(PzaUi.Install(new HarmonyLib.Harmony("pza.uiprobe.check")), "PzaUi.Install: every member resolves and every patch applies");
        Window(kenshi);
        Tiny();
        Faces(kenshi);
        Strings();
        Fit(kenshi);
        Console.WriteLine("-- log lines:");
        foreach (var l in lines) Console.WriteLine("  " + l);
        Console.WriteLine(s_bad == 0 ? "RESULT: PASS" : $"RESULT: FAIL ({s_bad})");
        return s_bad == 0 ? 0 : 1;
    }

    static void Expect(bool ok, string what)
    {
        Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {what}");
        if (!ok) s_bad++;
    }

    static void Manifest(string dir)
    {
        Console.WriteLine("== manifest and settings");
        var m = CodeModManifest.Read(Path.Combine(dir, "meitou.json"));
        Console.WriteLine($"manifest: id={m.Id} version={m.Version} entry={m.Entry} gameVersion={m.GameVersion}");
        CodeModVersion.TryParse("0.20.1", out var g);
        var problems = CodeModChecks.Check(new List<CodeModEntry> { new("PZAUiProbe", m, true) }, g);
        Expect(problems.Count == 0, "CodeModChecks against 0.20.1: " + (problems.Count == 0 ? "none" : string.Join("; ", problems.SelectMany(p => p.Value))));
        Expect(typeof(ProbeMod).Assembly.GetType(m.Entry) is { } t && typeof(Meitou.Modding.IMod).IsAssignableFrom(t), "entry is an IMod");
        Expect(File.Exists(Path.Combine(dir, "PZAUiProbe.mod")), "the data mod is staged");
        var groupType = typeof(Meitou.Modding.IMod).Assembly.GetType("Meitou.Modding.ModSettingsGroup")!;
        var ctor = groupType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).First(c => c.GetParameters().Length == 3);
        try { ctor.Invoke(new object[] { m.Id, "PZAUiProbe", new ProbeSettings() }); Expect(true, "the settings page accepts the probe's settings"); }
        catch (TargetInvocationException e) { Expect(false, "settings refused: " + e.InnerException?.Message); }
    }

    // the game's default binding for a screen it does not know (every Button without a TemplateEvent disabled), then the library's postfix
    static UiBindings Bind(MyGuiScreenHost host, string id)
    {
        var b = new UiBindings();
        foreach (var w in Walk(host.Layout(id).Widgets).Where(w => w.Type == "Button" && !w.Properties.ContainsKey("TemplateEvent"))) b.Disabled.Add(w.Name);
        PzaUi.Windows.FirstOrDefault(x => x.Id == id)?.ApplyBind(b);
        return b;
    }

    static IEnumerable<MyGuiWidget> Walk(IEnumerable<MyGuiWidget> ws)
    {
        foreach (var w in ws) { yield return w; foreach (var c in Walk(w.Children)) yield return c; }
    }

    static MyGuiResources Resources(string kenshi)
    {
        var files = Directory.EnumerateFiles(Path.Combine(kenshi, "data", "gui"), "*", SearchOption.AllDirectories)
            .GroupBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        return MyGuiResources.Load("kenshi_init.xml", n => files.GetValueOrDefault(Path.GetFileName(n)), "kenshi_fonts.xml");
    }

    static void Window(string kenshi)
    {
        Console.WriteLine("== PzaWindow on a real MyGuiScreenHost with Kenshi's resources");
        var res = Resources(kenshi);
        var host = new MyGuiScreenHost(res);
        const int W = 1920, H = 1080;
        void Frame() => PzaUi.Frame(host, W, H);
        IReadOnlyList<UiControl> Arrange() => host.Arrange(W, H, id => Bind(host, id)).Single(f => f.Id == Probe.Id).Controls;
        UiControl Ctl(string name) => Arrange().First(x => x.Name == name);
        Vector2 Mid(UiControl x) => new(x.Bounds.X + x.Bounds.Width / 2, x.Bounds.Y + x.Bounds.Height / 2);
        void Click(string name) { var p = Mid(Ctl(name)); host.PointerDown(p); host.PointerUp(); Frame(); }

        Frame();
        Expect(host.IsRegistered(Probe.Id) && !host.IsOpen(Probe.Id), "the first frame registers the window (closed)");
        Probe.Window.Toggle(); Frame();
        Expect(host.IsOpen(Probe.Id) && Probe.Window.IsOpen, "Toggle opens it on the next frame");

        var c = Arrange();
        var root = c.First(x => x.Type == "Window");
        Expect(root.Bounds.X >= 0 && root.Bounds.Y >= 0 && root.Bounds.X + root.Bounds.Width <= W && root.Bounds.Y + root.Bounds.Height <= H, $"on screen ({root.Bounds.X:0},{root.Bounds.Y:0} {root.Bounds.Width:0}x{root.Bounds.Height:0})");
        foreach (var name in new[] { "Tick", "lbTick", "PressButton", "CloseButton", "lbIntro", "lbSlider", "Slider", "Edit", "List", "Tabs@tab0", "Tabs@tab1" })
            Expect(c.Any(x => x.Name == name), $"PzaLayout made '{name}'");
        Expect(c.Where(x => x.Name is "Tick" or "PressButton" or "CloseButton").All(x => x.Enabled), "PzaWindow.Enable: our buttons enabled");
        Expect(c.Any(x => x.Name == "PressButton" && x.Properties.GetValueOrDefault("tooltip_text") == Probe.Strings.T("press.tip")), "PzaWindow.Tooltip reaches the control");
        Expect(c.Any(x => x.Name == "Tabs@tab0" && x.Caption == "WIDGETS"), "PzaWindow.TabText captions the tab");

        Click("PressButton"); Click("PressButton");
        Expect(Probe.Presses == 2 && Ctl("lbStatus").Caption.Contains("2"), $"PRESS: two events, the status follows ({Ctl("lbStatus").Caption})");
        Click("Tick");
        Expect(Probe.Tick && Ctl("Tick").Properties.GetValueOrDefault("Checked") == "true", "PzaWindow.Tick: the tick box shows its state");
        var rows = Arrange().Where(x => x.Action.StartsWith("mygui:list:", StringComparison.Ordinal)).ToList();
        if (rows.Count > 2) { host.PointerDown(Mid(rows[2])); host.PointerUp(); }
        Expect(Probe.Selected == 2, "PzaWindow.Lines + a click: line 2 selected");
        Click("Slider@End#1");
        Expect(Probe.SliderPos == 9, $"the slider's + arrow: position 9 ({Ctl("lbSlider").Caption})");
        Click("Edit");
        foreach (char ch in "Kenshi") host.TextInput(ch);
        Expect(Probe.Edited == "Kenshi" && PzaUi.Host!.HasKeyboardFocus, "typing reaches the mod; the box has the keyboard (PzaUi.Typing)");
        host.Blur();
        Click("Tabs@tab2");
        Expect(Arrange().Any(x => x.Name == "lbHelp") && !Arrange().Any(x => x.Name == "Tick"), "PzaLayout.HelpPage: the Help tab shows its page");
        Click("Tabs@tab0");

        // drag by the title: the place is saved as a fraction of the screen once the drag ends
        var r0 = Ctl("Root").Bounds;
        var grab = new Vector2(r0.X + r0.Width * 0.3f, r0.Y + 12);
        host.PointerDown(grab); host.PointerMove(grab + new Vector2(-384, 216)); Frame();
        Expect(Probe.Settings.WindowX == 0, "no save while the drag is on");
        host.PointerUp(); Frame();
        Expect(Math.Abs(Probe.Settings.WindowX + 0.2) < 0.002 && Math.Abs(Probe.Settings.WindowY - 0.2) < 0.002,
            $"after the drag: place saved as a fraction of the screen ({Probe.Settings.WindowX}, {Probe.Settings.WindowY})");

        // minimize (the user, 2026-11-16: "No minimize button for the UI menu thing"): Kenshi_WindowCXM's "-" and a title double click
        var minBtn = Arrange().FirstOrDefault(x => x.Skin == "Kenshi_MinimizeButtonSkin");
        Expect(minBtn is not null && minBtn.Action == PzaWindow.MinimizeAction && minBtn.Enabled, $"the window has Kenshi's minimize button, bound to our action ({minBtn?.Action})");
        long mins = Counters.Get("minimized");
        Click(minBtn!.Name);
        Expect(!host.IsOpen(Probe.Id) && Counters.Get("minimized") == mins + 1, "the - minimizes it (closed, Minimized called)");
        Probe.Window.Open(); Frame(); Arrange();   // the game lays the screens out every frame; the check does it here
        var hdr = host.LastFrame.First(f => f.Id == Probe.Id).Controls.First(x => x.Skin == "Kenshi_GenericWindowHeaderSkin").Bounds;
        var titleAt = new Vector2(hdr.X + hdr.Width * 0.3f, hdr.Y + hdr.Height / 2);
        System.Threading.Thread.Sleep(450);
        PzaUi.PointerDown(MouseButton.Left, titleAt); Frame(); Arrange();
        Expect(host.IsOpen(Probe.Id), "one press on the title: still open (a drag starts this way)");
        System.Threading.Thread.Sleep(450);
        PzaUi.PointerDown(MouseButton.Left, titleAt); Frame(); Arrange();
        Expect(host.IsOpen(Probe.Id), "two slow presses: still open");
        PzaUi.PointerDown(MouseButton.Left, titleAt); Frame();
        Expect(!host.IsOpen(Probe.Id) && Counters.Get("minimized") == mins + 2, "a double click on the title minimizes it");
        Probe.Window.Open(); Frame();
        var closeBtn = Arrange().First(x => x.Skin == "Kenshi_CloseButtonSkin");
        System.Threading.Thread.Sleep(450);
        var cb = new Vector2(closeBtn.Bounds.X + 5, closeBtn.Bounds.Y + 5);
        PzaUi.PointerDown(MouseButton.Left, cb); PzaUi.PointerDown(MouseButton.Left, cb); Frame();
        Expect(host.IsOpen(Probe.Id), "two presses on the header's close button are not a title double click");

        Click("CloseButton");
        Expect(!host.IsOpen(Probe.Id), "CLOSE closes it (through PzaWindow.Close)");

        // a restart at another resolution: a new host, the place restored from the settings
        var host2 = new MyGuiScreenHost(res);
        PzaUi.Frame(host2, 3840, 2160);
        Probe.Window.Open(); PzaUi.Frame(host2, 3840, 2160);
        var off = host2.GetOffset(Probe.Id);
        Expect(host2.IsOpen(Probe.Id) && Math.Abs(off.X + 768) < 2 && Math.Abs(off.Y - 432) < 2, $"a new host at 4K opens it at the saved place (offset {off.X:0},{off.Y:0} = -0.2, 0.2 of 3840x2160)");
        Expect(host2.Escape() && !host2.IsOpen(Probe.Id), "Escape closes it");
        PzaUi.Frame(host2, 3840, 2160);
    }

    static void Tiny()
    {
        Console.WriteLine("== PzaTinyButton through Meitou's real UiHost");
        var ui = new UiHost();
        using var reg = ui.Register(new PzaHud("pza.uiprobe"));
        const int W = 1920, H = 1080;
        var slots = new Dictionary<string, UiRect> { [UiSlots.Overlay] = new(0, 0, W, H) };
        IReadOnlyList<UiControl> Arrange() => ui.Arrange(UiMode.Meitou, W, H, new UiBindings(), slots);
        var t = Probe.Tiny;
        var L = MouseButton.Left;

        var c = Arrange();
        var btn = c.FirstOrDefault(x => x.Caption == "UI PROBE");
        var plus = c.FirstOrDefault(x => x.Caption == "+");
        Expect(btn is not null && plus is not null && btn.Action.Length == 0, "laid out: the button (no press action) and its +");
        Expect(btn!.Bounds.X + btn.Bounds.Width + 4 + plus!.Bounds.Width <= W + 0.5f && btn.Bounds.X > W / 2, $"in the top-right corner, on screen ({btn.Bounds.X:0},{btn.Bounds.Y:0})");

        var host = PzaUi.Host!;
        bool wasOpen = Probe.Window.IsOpen;
        var mid = new Vector2(btn.Bounds.X + 10, btn.Bounds.Y + 10);
        t.PointerDown(L, mid); t.PointerUp(L, mid + new Vector2(2, 0));
        PzaUi.Frame(host, 3840, 2160);
        Expect(Probe.Window.IsOpen != wasOpen, "a click (no drag) toggles the window");
        Expect(!Arrange().Any(x => x.Caption == "UI PROBE"), "while the window is open the tiny button is hidden (Visible = !IsOpen)");
        Probe.Window.Close(); PzaUi.Frame(host, 3840, 2160);
        Expect(Arrange().Any(x => x.Caption == "UI PROBE"), "closed again: the tiny button is back");

        int clicks = (int)Counters.Get("tiny.click");
        t.PointerDown(L, mid); t.PointerMove(mid + new Vector2(-600, 400)); t.PointerUp(L, mid + new Vector2(-600, 400));
        var moved = Arrange().First(x => x.Caption == "UI PROBE").Bounds;
        Expect(Counters.Get("tiny.click") == clicks, "a drag does not click");
        Expect(Math.Abs(moved.X - (btn.Bounds.X - 600)) <= 2 && Math.Abs(moved.Y - (btn.Bounds.Y + 400)) <= 2,
            $"the drag moved it and saved its place ({moved.X:0},{moved.Y:0}; {Probe.Settings.TinyX}, {Probe.Settings.TinyY})");

        Probe.Settings.TinyOpacity = 1;
        var all = t.AllRect!.Value;
        bool ours = true;
        for (int i = 0; i < 9; i++) ours &= t.Scroll(new Vector2(all.X + 3, all.Y + 3), -1);
        Expect(ours && Math.Abs(Probe.Settings.TinyOpacity - 0.35) < 1e-9, $"the wheel over it is ours and stops at 0.35 ({Probe.Settings.TinyOpacity})");
        Expect(!t.Scroll(new Vector2(5, H - 5), 1), "the wheel elsewhere stays the game's (zoom)");

        bool before = Probe.Window.IsOpen;
        if (before) { Probe.Window.Close(); PzaUi.Frame(host, 3840, 2160); }
        var p2 = Arrange().First(x => x.Caption == "+");
        Expect(ui.Dispatch(p2), "the + dispatches");
        PzaUi.Frame(host, 3840, 2160);
        Expect(Probe.Window.IsOpen, "the + opens the window");
    }

    static void Faces(string kenshi)
    {
        Console.WriteLine("== PzaPortrait (character faces)");
        Expect(PzaPortrait.Available, "faces available: GameHost.GatherPortraitKeys, PortraitKey, CharacterRenderer.PortraitRequests and PortraitBar resolve; the prefix applies");
        PzaPortrait.Reset();
        CharacterId a = new(3, 1), b2 = new(7, 2);
        Probe.FacesFor = () => [a, b2];
        var host = new MyGuiScreenHost(Resources(kenshi));
        const int W = 1920, H = 1080;
        PzaUi.Frame(host, W, H);
        Probe.Window.Open(); PzaUi.Frame(host, W, H);
        IReadOnlyList<UiControl> Arrange() => host.Arrange(W, H, id => Bind(host, id)).Single(f => f.Id == Probe.Id).Controls;
        var tab = Arrange().First(x => x.Name == "Tabs@tab1");
        host.PointerDown(new Vector2(tab.Bounds.X + tab.Bounds.Width / 2, tab.Bounds.Y + tab.Bounds.Height / 2)); host.PointerUp(); PzaUi.Frame(host, W, H);
        var c = Arrange();
        UiControl? F(int i) => c.FirstOrDefault(x => x.Name == "Face" + i);
        Expect(c.Any(x => x.Name == "lbFaces") && F(0) is not null && F(3) is not null, "the Faces tab shows its text and four portrait frames");
        Expect(F(0)?.Bounds is { Width: > 40 } r0 && Math.Abs(r0.Width - r0.Height) <= 2, $"a portrait frame is square on screen ({F(0)?.Bounds.Width:0}x{F(0)?.Bounds.Height:0})");
        Expect(F(0)?.Properties.GetValueOrDefault("ImageTexture") == "meitou:portraits" && F(0)?.Properties.GetValueOrDefault("ImageFlipV") == "true"
            && F(0)?.Properties.GetValueOrDefault("ImageRect") == "3840 768 128 128", $"face 0 shows atlas cell 63 ({F(0)?.Properties.GetValueOrDefault("ImageRect")})");
        Expect(F(1)?.Properties.GetValueOrDefault("ImageRect") == "3584 768 128 128", $"face 1 shows cell 62 ({F(1)?.Properties.GetValueOrDefault("ImageRect")})");
        Expect(F(2)?.Properties.ContainsKey("ImageTexture") == false, "an empty frame shows nothing");
        Expect(MyGuiImages.Resolve(Resources(kenshi), F(0)!.Properties) is { Texture: "meitou:portraits" } img && img.Source.Width == 128,
            "Meitou's own image resolver reads the face's texture and rectangle");

        long Key(CharacterId id) => 1000 + id.Slot;
        uint Serial(CharacterId id) => 5;
        var req = new List<PortraitRequest> { new(1, 0, 0), new(2, 33, 0) };
        PzaPortrait.AddRequests(req, Key, Serial);
        Expect(req.Count == 4 && req.Any(r => r.Key == 1003 && r.Cell == 63 && r.Serial == 5) && req.Any(r => r.Key == 1007 && r.Cell == 62),
            "ours are added: cells 63 and 62, the game's key and serial");
        PzaPortrait.AddRequests(req, Key, Serial);
        Expect(req.Count == 4, "a second gather in the same frame adds nothing twice");
        var next = new List<PortraitRequest> { new(99, 63, 0) };   // another request (a record portrait, another mod) takes cell 63
        PzaPortrait.AddRequests(next, Key, Serial);
        int moved = PzaPortrait.CellOf(a);
        Expect(moved is >= 32 and < 62 && next.Count == 3 && next.Count(r => r.Cell == 63) == 1, $"a face whose cell is taken moves to a free one ({moved})");
        c = Arrange();
        Expect(F(0)?.Properties.GetValueOrDefault("ImageRect") is { } rr && rr.StartsWith(((moved % 16) * 256).ToString(System.Globalization.CultureInfo.InvariantCulture) + " "),
            $"the window follows the move on its next bind ({F(0)?.Properties.GetValueOrDefault("ImageRect")})");
        Probe.FacesFor = () => [];
        Probe.Window.Close(); PzaUi.Frame(host, W, H);
        for (int i = 0; i < 12; i++) PzaPortrait.AddRequests(new List<PortraitRequest>(), Key, Serial);
        var after = new List<PortraitRequest>();
        PzaPortrait.AddRequests(after, Key, Serial);
        Expect(after.Count == 0 && PzaPortrait.CellOf(a) == -1, "faces no longer bound give their cells up");
    }

    /// <summary>PZA's Meitou UI's text-fit check (PzaFitCheck, run by every PZA Meitou mod's check): the probe window in four languages at three
    /// screen sizes, then a planted too-long label that it must report by widget, language and text, then the fit pass cutting it at run time.</summary>
    static void Fit(string kenshi)
    {
        Console.WriteLine("== text fit (PzaFitCheck): every text of the window, en / ru / zh / ja, three screen sizes");
        var res = Resources(kenshi);
        var probeDir = Path.GetDirectoryName(typeof(ProbeMod).Assembly.Location)!;
        void Lang(string l) { Probe.Strings.Init(probeDir, l, "auto", "lang/{code}.txt"); }
        Console.WriteLine("== PzaFont against FreeType (the engine Meitou draws with), four languages, three screen heights");
        Expect(FontCalibration.Run(kenshi, Expect) == 0, "PzaFont never measures narrower than FreeType (by more than a pixel), never more than 6% wider");
        var found = PzaFitCheck.Run(kenshi, res, Probe.Window, Bind, Lang, Probe.Strings.KeyOf);
        Expect(found.Count == 0, $"every text fits its widget in four languages ({found.Count} overflow(s))");

        Console.WriteLine("== text fit: the planted too-long label must fail, named");
        UiBindings Planted(MyGuiScreenHost h, string id) { var b = Bind(h, id); if (id == Probe.Id) b.Text["lbStatus"] = PzaFitCheck.PlantedText; return b; }
        var planted = PzaFitCheck.Run(kenshi, res, Probe.Window, Planted, Lang, Probe.Strings.KeyOf, s => { }, ["en", "ja"]);
        var hit = planted.Where(o => o.Widget == "lbStatus" && o.Text == PzaFitCheck.PlantedText).ToList();
        Expect(hit.Select(o => o.Lang).Distinct().Count() == 2 && hit.All(o => o.Why.Contains(" px wide in ")),
            $"the planted label is reported in en and ja: {hit.FirstOrDefault()}");

        Console.WriteLine("== text fit: at run time the fit pass cuts it and puts the whole text in the tooltip");
        PzaFit.KenshiDir = kenshi; PzaFit.Lang = "en";
        var host = new MyGuiScreenHost(res);
        const int W = 1920, H = 1080;
        var modBind = Probe.Window.Bind;
        Probe.Window.Bind = b => { modBind?.Invoke(b); b.Text["lbStatus"] = PzaFitCheck.PlantedText; };   // planted inside the mod's own bind
        PzaUi.Frame(host, W, H); Probe.Window.Open(); PzaUi.Frame(host, W, H);
        host.Arrange(W, H, id => Bind(host, id));
        var c = host.Arrange(W, H, id => Bind(host, id)).Single(f => f.Id == Probe.Id).Controls.First(x => x.Name == "lbStatus");
        Probe.Window.Bind = modBind;
        Expect(c.Caption.EndsWith("...", StringComparison.Ordinal) && c.Properties.GetValueOrDefault("tooltip_text") == PzaFitCheck.PlantedText,
            $"cut to '{c.Caption}', tooltip holds the whole text");
        Probe.Window.Close(); PzaUi.Frame(host, W, H);
        PzaFit.KenshiDir = null;
    }

    static void Strings()
    {
        Console.WriteLine("== PzaStrings");
        var into = new Dictionary<string, string>();
        var (used, refused) = Probe.Strings.Load(System.Text.Encoding.UTF8.GetBytes("slider = no number\nstatus.some = {1} раз\ntitle = \\q\nnot.ours = x\n"), into);
        Expect(used == 1 && refused.Count == 2 && into.ContainsKey("status.some"), "a lost placeholder and a bad escape refused, unknown keys skipped: " + string.Join("; ", refused));
        Expect(Probe.Strings.TF("status.some", "3") == "Pressed 3 time(s)", "TF fills {1} (English, no file loaded)");
    }
}
