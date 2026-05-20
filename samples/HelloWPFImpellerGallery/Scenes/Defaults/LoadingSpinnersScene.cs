using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class LoadingSpinnersScene : IGalleryScene
{
    public string Name => "Loading Spinners";
    public string? Description => "Common loading-indicator patterns animated with the frame clock";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);
        float t = (float)e.TotalTime.TotalSeconds;

        float[] cxs = { e.PixelWidth * 0.18f, e.PixelWidth * 0.50f, e.PixelWidth * 0.82f };
        float[] cys = { e.PixelHeight * 0.30f, e.PixelHeight * 0.70f };

        // 1. Rotating arc
        DrawArcSpinner(b, cxs[0], cys[0], 50 * e.DpiScaleX, t * 240f);
        // 2. Dot ring fade
        DrawDotRing(b, cxs[1], cys[0], 50 * e.DpiScaleX, t);
        // 3. Pulsing dots
        DrawPulsingDots(b, cxs[2], cys[0], 50 * e.DpiScaleX, t);
        // 4. Bouncing bars
        DrawBouncingBars(b, cxs[0], cys[1], 50 * e.DpiScaleX, t);
        // 5. Ring trail
        DrawRingTrail(b, cxs[1], cys[1], 50 * e.DpiScaleX, t);
        // 6. Orbiting balls
        DrawOrbitingBalls(b, cxs[2], cys[1], 50 * e.DpiScaleX, t);
    }

    private static void DrawArcSpinner(ImpellerDisplayListBuilder b, float cx, float cy, float r, float angleDeg)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x6F, 0xC2, 0xE8));
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(6);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);

        const int segs = 28;
        const float sweep = MathF.PI * 1.2f;
        float a0 = angleDeg * MathF.PI / 180;
        using var pb = ImpellerPathBuilder.New()!;
        for (int i = 0; i <= segs; i++)
        {
            float aa = a0 + sweep * i / segs;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(aa) * r, Y = cy + MathF.Sin(aa) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }

    private static void DrawDotRing(ImpellerDisplayListBuilder b, float cx, float cy, float r, float t)
    {
        const int n = 12;
        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < n; i++)
        {
            float phase = (i / (float)n - t * 0.5f);
            phase -= MathF.Floor(phase);
            float alpha = 1f - phase;
            p.SetColor(new ImpellerColor { Alpha = alpha, Red = 0.9f, Green = 0.6f, Blue = 0.4f });
            float ang = i * MathF.PI * 2 / n;
            float x = cx + MathF.Cos(ang) * r;
            float y = cy + MathF.Sin(ang) * r;
            b.DrawOval(new ImpellerRect((int)(x - 6), (int)(y - 6), 12, 12), p);
        }
    }

    private static void DrawPulsingDots(ImpellerDisplayListBuilder b, float cx, float cy, float r, float t)
    {
        const int n = 3;
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0xB8, 0xE8, 0x6F));
        for (int i = 0; i < n; i++)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(t * 4 + i * 0.8f);
            float sz = 8 + 10 * pulse;
            float x = cx - 36 + i * 36;
            b.DrawOval(new ImpellerRect((int)(x - sz / 2), (int)(cy - sz / 2), (int)sz, (int)sz), p);
        }
    }

    private static void DrawBouncingBars(ImpellerDisplayListBuilder b, float cx, float cy, float r, float t)
    {
        const int n = 5;
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0xE8, 0x70, 0xC8));
        var radii = SceneHelpers.UniformRadii(3);
        for (int i = 0; i < n; i++)
        {
            float phase = t * 6 + i * 0.4f;
            float h = (20 + 30 * (0.5f + 0.5f * MathF.Sin(phase)));
            float w = 10;
            float x = cx - (n * (w + 4)) / 2 + i * (w + 4);
            float y = cy - h / 2;
            b.DrawRoundedRect(new ImpellerRect((int)x, (int)y, (int)w, (int)h), radii, p);
        }
    }

    private static void DrawRingTrail(ImpellerDisplayListBuilder b, float cx, float cy, float r, float t)
    {
        using var p = ImpellerPaint.New()!;
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(5);
        p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapRound);

        // Track
        p.SetColor(new ImpellerColor { Alpha = 0.15f, Red = 1, Green = 1, Blue = 1 });
        b.DrawOval(new ImpellerRect((int)(cx - r), (int)(cy - r), (int)(r * 2), (int)(r * 2)), p);

        // Animated trail (using a path arc)
        p.SetColor(ImpellerColor.FromRgb(0xE8, 0xCB, 0x6F));
        const int segs = 28;
        float trailLen = MathF.PI * 0.6f;
        float a0 = t * 2.5f;
        using var pb = ImpellerPathBuilder.New()!;
        for (int i = 0; i <= segs; i++)
        {
            float aa = a0 + trailLen * i / segs;
            var pt = new ImpellerPoint { X = cx + MathF.Cos(aa) * r, Y = cy + MathF.Sin(aa) * r };
            if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
        }
        using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
        b.DrawPath(path, p);
    }

    private static void DrawOrbitingBalls(ImpellerDisplayListBuilder b, float cx, float cy, float r, float t)
    {
        const int n = 3;
        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < n; i++)
        {
            float ang = t * 2 + i * MathF.PI * 2 / n;
            float x = cx + MathF.Cos(ang) * r;
            float y = cy + MathF.Sin(ang) * r;
            var (cr, cg, cbb) = SceneHelpers.HsvToRgb(i / (float)n, 0.85f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 1, Red = cr, Green = cg, Blue = cbb });
            b.DrawOval(new ImpellerRect((int)(x - 10), (int)(y - 10), 20, 20), p);
        }
    }
}
