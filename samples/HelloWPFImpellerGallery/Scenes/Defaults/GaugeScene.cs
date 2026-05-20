using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class GaugeScene : IGalleryScene
{
    public string Name => "Gauge / Speedometer";
    public string? Description => "Half-circle gauge with animated needle, tick marks, value readout";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x18, 0x1C, 0x24);
        float t = (float)e.TotalTime.TotalSeconds;

        float cx = e.PixelWidth / 2f;
        float cy = e.PixelHeight * 0.62f;
        float radius = MathF.Min(cx, e.PixelHeight * 0.42f);

        // Background arc
        DrawHalfArc(b, cx, cy, radius, 0xE8, 0xE8, 0xE8, alpha: 0.10f, thickness: 26f * e.DpiScaleX);

        // Value fill arc (animated)
        float value = 0.5f + 0.5f * MathF.Sin(t * 0.6f); // 0..1
        DrawColoredArc(b, cx, cy, radius, value, 26f * e.DpiScaleX);

        // Ticks
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(ImpellerColor.FromRgb(0xC0, 0xC8, 0xD0));
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
            for (int i = 0; i <= 10; i++)
            {
                bool major = i % 2 == 0;
                p.SetStrokeWidth((major ? 3f : 1.5f) * e.DpiScaleX);
                float ang = MathF.PI + i * MathF.PI / 10;
                float r0 = radius - 38 * e.DpiScaleX;
                float r1 = r0 - (major ? 14 * e.DpiScaleX : 7 * e.DpiScaleX);
                b.DrawLine(
                    new ImpellerPoint { X = cx + MathF.Cos(ang) * r0, Y = cy + MathF.Sin(ang) * r0 },
                    new ImpellerPoint { X = cx + MathF.Cos(ang) * r1, Y = cy + MathF.Sin(ang) * r1 },
                    p);
            }
        }

        // Needle
        using (var p = ImpellerPaint.New()!)
        {
            float ang = MathF.PI + value * MathF.PI;
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0x6F, 0x6F));
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(5f * e.DpiScaleX);
            p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
            b.DrawLine(
                new ImpellerPoint { X = cx, Y = cy },
                new ImpellerPoint { X = cx + MathF.Cos(ang) * (radius - 50 * e.DpiScaleY), Y = cy + MathF.Sin(ang) * (radius - 50 * e.DpiScaleY) },
                p);

            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleFill);
            p.SetColor(ImpellerColor.FromRgb(0x2D, 0x32, 0x3C));
            b.DrawOval(new ImpellerRect((int)(cx - 12 * e.DpiScaleY), (int)(cy - 12 * e.DpiScaleY), (int)(24 * e.DpiScaleY), (int)(24 * e.DpiScaleY)), p);
        }

        // Value readout
        if (e.Typography != null)
        {
            int pct = (int)(value * 100);
            TextBasicsScene.DrawSimpleText(b, e.Typography, $"{pct}%",
                40 * e.DpiScaleX, cx - 100, cy + 30 * e.DpiScaleY, 200,
                ImpellerColor.FromRgb(0xF0, 0xF0, 0xF0),
                weight: ImpellerFontWeight.kImpellerFontWeight700,
                align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }

    private static void DrawHalfArc(ImpellerDisplayListBuilder b, float cx, float cy, float r, byte rc, byte gc, byte bc, float alpha, float thickness)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(new ImpellerColor { Alpha = alpha, Red = rc / 255f, Green = gc / 255f, Blue = bc / 255f });
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(thickness);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
        using var pb = ImpellerPathBuilder.New()!;
        const int segs = 64;
        for (int i = 0; i <= segs; i++)
        {
            float ang = MathF.PI + i * MathF.PI / segs;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(ang) * r, Y = cy + MathF.Sin(ang) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }

    private static void DrawColoredArc(ImpellerDisplayListBuilder b, float cx, float cy, float r, float fill01, float thickness)
    {
        using var p = ImpellerPaint.New()!;
        // Color goes green -> yellow -> red as fill grows
        var (cr, cg, cbb) = SceneHelpers.HsvToRgb((1f - fill01) * 0.33f, 0.85f, 1.0f);
        p.SetColor(new ImpellerColor { Alpha = 1, Red = cr, Green = cg, Blue = cbb });
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(thickness);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);
        using var pb = ImpellerPathBuilder.New()!;
        const int segs = 96;
        int n = (int)MathF.Max(1, segs * fill01);
        for (int i = 0; i <= n; i++)
        {
            float ang = MathF.PI + i * MathF.PI / segs;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(ang) * r, Y = cy + MathF.Sin(ang) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }
}
