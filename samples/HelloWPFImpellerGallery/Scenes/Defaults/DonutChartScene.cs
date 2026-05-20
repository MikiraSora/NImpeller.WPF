using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class DonutChartScene : IGalleryScene
{
    public string Name => "Donut Chart";
    public string? Description => "Pie chart with center hole — drawn as stroked arcs";

    private readonly (string label, float value)[] _data =
    {
        ("Code",     42),
        ("Review",   18),
        ("Meetings", 22),
        ("Email",    11),
        ("Coffee",    7),
    };

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x18, 0x1C, 0x24);
        float t = (float)e.TotalTime.TotalSeconds;

        float total = 0;
        foreach (var d in _data) total += d.value;

        float cx = e.PixelWidth * 0.35f;
        float cy = e.PixelHeight * 0.5f;
        float radius = MathF.Min(e.PixelWidth, e.PixelHeight) * 0.30f;
        float thickness = radius * 0.30f;

        float anim = MathF.Min(1f, t * 0.5f);
        anim = 1f - MathF.Pow(1f - anim, 3);

        float startAng = -MathF.PI / 2;
        for (int i = 0; i < _data.Length; i++)
        {
            float sweep = _data[i].value / total * MathF.PI * 2 * anim;
            DrawArcRing(b, cx, cy, radius, thickness, startAng, sweep, i / (float)_data.Length);
            startAng += _data[i].value / total * MathF.PI * 2;
        }

        // Center label
        if (e.Typography != null)
        {
            TextBasicsScene.DrawSimpleText(b, e.Typography, "100h",
                36 * e.DpiScaleY, cx - 100, cy - 28 * e.DpiScaleY, 200,
                ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF),
                weight: ImpellerFontWeight.kImpellerFontWeight700,
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
            TextBasicsScene.DrawSimpleText(b, e.Typography, "this week",
                14 * e.DpiScaleY, cx - 100, cy + 18 * e.DpiScaleY, 200,
                ImpellerColor.FromRgb(0xA0, 0xA8, 0xB2),
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);

            // Legend
            float legendX = e.PixelWidth * 0.62f;
            float legendY = e.PixelHeight * 0.30f;
            using var sw = ImpellerPaint.New()!;
            for (int i = 0; i < _data.Length; i++)
            {
                var (rr, gg, bb) = SceneHelpers.HsvToRgb(i / (float)_data.Length, 0.7f, 1.0f);
                sw.SetColor(new ImpellerColor { Alpha = 1, Red = rr, Green = gg, Blue = bb });
                b.DrawOval(new ImpellerRect((int)legendX, (int)(legendY + i * 42 * e.DpiScaleY), (int)(20 * e.DpiScaleY), (int)(20 * e.DpiScaleY)), sw);
                TextBasicsScene.DrawSimpleText(b, e.Typography,
                    $"{_data[i].label}  —  {_data[i].value:0}h",
                    16 * e.DpiScaleY, legendX + 32 * e.DpiScaleY, legendY + i * 42 * e.DpiScaleY, e.PixelWidth,
                    ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8));
            }
        }
    }

    private static void DrawArcRing(ImpellerDisplayListBuilder b, float cx, float cy, float r, float thickness, float a0, float sweep, float hue)
    {
        if (sweep <= 0) return;
        using var p = ImpellerPaint.New()!;
        var (cr, cg, cbb) = SceneHelpers.HsvToRgb(hue, 0.75f, 1.0f);
        p.SetColor(new ImpellerColor { Alpha = 1, Red = cr, Green = cg, Blue = cbb });
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(thickness);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapButt);

        using var pb = ImpellerPathBuilder.New()!;
        const int segs = 96;
        for (int i = 0; i <= segs; i++)
        {
            float aa = a0 + sweep * i / segs;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(aa) * r, Y = cy + MathF.Sin(aa) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }
}
