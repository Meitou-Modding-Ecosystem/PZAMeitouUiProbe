// PZA's UI Probe offline check: PzaFont (PZA's Meitou UI's own font measuring, which every mod's fit check uses) against FreeType, the engine Meitou
// draws with (the FreeTypeSharp + freetype.dll of the Meitou release), set up the way Meitou sets it up: TrueType interpreter 35, the
// resource's points at its dpi in 1/16 steps and hinted advances (Faithful), whole-pixel em and unhinted advances (Meitou's look).
// Falsifier: a sample where PzaFont measures narrower than FreeType by more than a pixel (the fit check would pass text that does not fit).
// Author PZA, GPLv3 or later. Never shipped.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FreeTypeSharp;
using PZA.MeitouUi;
using static FreeTypeSharp.FT;

static unsafe class FontCalibration
{
    static FT_LibraryRec_* s_lib;

    static readonly (string Lang, string[] Samples)[] Samples =
    [
        ("en", ["PZA's WASD Control is ON (Shift+K turns it off)", "Liveliness: one setting for how lively everything moves", "MMMMWWWW iiiillll 0123456789"]),
        ("ru", ["Плавное замедление времени для драматичных моментов", "Скорость: 0,30x — Shift+L включает и выключает"]),
        ("zh", ["慢动作让整个游戏为戏剧性时刻放慢，然后恢复。", "按 Shift+L 开关慢动作（输入文字时无效）"]),
        ("ja", ["スローモーションでゲーム全体を遅くし、元に戻します。", "Shift+L でオン・オフ（文字入力中は無効）"]),
    ];

    /// <summary>Returns the number of samples PzaFont measures narrower than FreeType (by more than a pixel); prints every comparison.</summary>
    public static int Run(string kenshi, Action<bool, string> expect)
    {
        int bad = 0;
        FT_LibraryRec_* lib;
        var e = FT_Init_FreeType(&lib);
        s_lib = lib;
        if (e != FT_Error.FT_Err_Ok) { expect(false, "FreeType did not start: " + e); return 1; }
        int version = 35;
        fixed (byte* module = "truetype"u8)
        fixed (byte* property = "interpreter-version"u8)
            FT_Property_Set(s_lib, module, property, &version);
        foreach (var (lang, samples) in Samples)
            foreach (int h in new[] { 720, 1080, 1440 })
            {
                var font = PzaFont.FromInstall(kenshi, lang, PzaFit.StandardFont, h);
                if (font is null) { expect(false, $"[{lang}] no font"); bad++; continue; }
                foreach (var s in samples)
                {
                    float faithful = Measure(font.FilePath, (long)MathF.Max(1, MathF.Round(font.EmFaithful * 72 / font.Resolution * 16)) * 4, (uint)MathF.Round(font.Resolution), true, s);
                    float meitou = Measure(font.FilePath, (long)font.EmMeitou * 64, 72, false, s);
                    if (Environment.GetEnvironmentVariable("PZA_CAL") == "1")
                    {
                        float em = font.EmFaithful;
                        float A = Lin(font.FilePath, s, MathF.Ceiling(em), 0), B = Lin(font.FilePath, s, MathF.Round(em), 0), C = Lin(font.FilePath, s, em, 1);
                        Console.WriteLine($"   CAL [{lang} {h}] em {em:0.00} FT-faithful {faithful:0} A(ceil ppem) {A:0} B(round ppem) {B:0} C(ceil adv) {C:0}");
                    }
                    float ft = MathF.Ceiling(Math.Max(faithful, meitou)), ours = font.Width(s);
                    bool ok = ours >= ft - 1 && ours <= ft * 1.06f + 2;
                    if (!ok) bad++;
                    expect(ok, $"[{lang} {h}p] PzaFont {ours:0} px vs FreeType {ft:0} px (Faithful {faithful:0}, Meitou {meitou:0}; {Path.GetFileName(font.FilePath)}): \"{s}\"");
                }
            }
        return bad;
    }

    static float Lin(string path, string text, float em, int mode)
    {
        Measure(path, 64, 72, false, "a");
        var face = (FT_FaceRec_*)s_faces[path];
        float w = 0; int upem = face->units_per_EM;
        foreach (var r in text.EnumerateRunes())
        {
            uint g = FT_Get_Char_Index(face, (UIntPtr)r.Value);
            FT_Load_Glyph(face, g, FT_LOAD.FT_LOAD_NO_SCALE);
            float a = face->glyph->advance.x * em / upem;
            w += mode == 0 ? MathF.Round(a) : MathF.Ceiling(a);
        }
        return w;
    }

    static readonly Dictionary<string, IntPtr> s_faces = new(StringComparer.OrdinalIgnoreCase);

    static float Measure(string path, long size64, uint dpi, bool hinted, string text)
    {
        if (!s_faces.TryGetValue(path, out var fp))
        {
            FT_FaceRec_* f;
            byte[] data = File.ReadAllBytes(path);
            byte* mem = (byte*)System.Runtime.InteropServices.NativeMemory.Alloc((nuint)data.Length);
            data.AsSpan().CopyTo(new Span<byte>(mem, data.Length));
            if (FT_New_Memory_Face(s_lib, mem, data.Length, 0, &f) != FT_Error.FT_Err_Ok) return float.NaN;
            s_faces[path] = fp = (IntPtr)f;
        }
        var face = (FT_FaceRec_*)fp;
        FT_Set_Char_Size(face, 0, (IntPtr)size64, dpi, dpi);
        float w = 0;
        foreach (var r in text.EnumerateRunes())
        {
            uint g = FT_Get_Char_Index(face, (UIntPtr)r.Value);
            var flags = hinted ? FT_LOAD.FT_LOAD_DEFAULT : FT_LOAD.FT_LOAD_NO_HINTING;
            if (FT_Load_Glyph(face, g, flags) != FT_Error.FT_Err_Ok && FT_Load_Glyph(face, g, FT_LOAD.FT_LOAD_FORCE_AUTOHINT) != FT_Error.FT_Err_Ok) continue;
            var slot = face->glyph;
            w += hinted ? slot->advance.x / 64f : (slot->linearHoriAdvance != 0 ? slot->linearHoriAdvance / 65536f : slot->advance.x / 64f);
        }
        return w;
    }
}
