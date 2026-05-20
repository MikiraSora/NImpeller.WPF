using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestMixedPipelineScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Mixed Pipeline";
    public override string? Description => "Mixed: N×2 rects + N×0.2 paths + N×0.05 shadows + N×0.03 blurs + N×0.08 text labels";
    public override string ItemLabel => "× scale (base 1000)";

    public StressTestMixedPipelineScene() : base(initial: 1000, step: 250, min: 0, max: 20000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x10, 0x12, 0x18);

        var rng = StressHelpers.Seeded(11);
        float t = (float)e.TotalTime.TotalSeconds;
        var radii = SceneHelpers.UniformRadii(4);

        int n = ItemCount;
        int nRects   = n * 2;
        int nPaths   = n / 5;
        int nShadows = n / 20;
        int nBlurs   = Math.Max(1, n / 33);
        int nText    = n / 12;

        // Rects
        using (var p = ImpellerPaint.New()!)
        {
            for (int i = 0; i < nRects; i++)
            {
                int x = rng.Next(e.PixelWidth);
                int y = rng.Next(e.PixelHeight);
                int sz = 4 + rng.Next(8);
                float hue = (i * 0.001f + t * 0.05f) % 1f;
                var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
                p.SetColor(new ImpellerColor { Alpha = 0.6f, Red = rr, Green = gg, Blue = bb });
                b.DrawRect(new ImpellerRect(x, y, sz, sz), p);
            }
        }

        // Cubic paths
        using (var p = ImpellerPaint.New()!)
        {
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(2f);
            for (int i = 0; i < nPaths; i++)
            {
                float cx = rng.Next(e.PixelWidth);
                float cy = rng.Next(e.PixelHeight);
                float r = 10 + rng.Next(20);
                using var pb = ImpellerPathBuilder.New()!;
                pb.MoveTo(new ImpellerPoint { X = cx - r, Y = cy });
                pb.QuadraticCurveTo(new ImpellerPoint { X = cx, Y = cy - r * 2 },
                                    new ImpellerPoint { X = cx + r, Y = cy });
                using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                float hue = (i * 0.005f + t * 0.05f) % 1f;
                var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.8f, 1.0f);
                p.SetColor(new ImpellerColor { Alpha = 0.85f, Red = rr, Green = gg, Blue = bb });
                b.DrawPath(path, p);
            }
        }

        // Shadows
        var shadowColor = ImpellerColor.FromRgb(0x00, 0x00, 0x00);
        using (var fill = ImpellerPaint.New()!)
        {
            for (int i = 0; i < nShadows; i++)
            {
                int x = rng.Next(e.PixelWidth - 80);
                int y = rng.Next(e.PixelHeight - 60);
                int w = 50 + rng.Next(40);
                int h = 30 + rng.Next(30);
                using (var pb = ImpellerPathBuilder.New()!)
                {
                    pb.AddRoundedRect(new ImpellerRect(x, y, w, h), radii);
                    using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                    b.DrawShadow(path, shadowColor, 6f, 0, (float)e.DpiScaleX);
                }
                fill.SetColor(ImpellerColor.FromRgb(0xE8, 0xE8, 0xF0));
                b.DrawRoundedRect(new ImpellerRect(x, y, w, h), radii, fill);
            }
        }

        // Blurred ovals
        for (int i = 0; i < nBlurs; i++)
        {
            int x = rng.Next(e.PixelWidth);
            int y = rng.Next(e.PixelHeight);
            int sz = 40 + rng.Next(50);
            using var mask = ImpellerMaskFilter.CreateBlurNew(ImpellerBlurStyle.kImpellerBlurStyleNormal, 8f)!;
            using var p = ImpellerPaint.New()!;
            float hue = (i * 0.03f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.8f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.4f, Red = rr, Green = gg, Blue = bb });
            p.SetMaskFilter(mask);
            b.DrawOval(new ImpellerRect(x, y, sz, sz), p);
        }

        // Text labels
        if (e.Typography != null)
        {
            for (int i = 0; i < nText; i++)
            {
                int x = rng.Next(e.PixelWidth - 80);
                int y = rng.Next(e.PixelHeight - 20);
                TextBasicsScene.DrawSimpleText(b, e.Typography, $"#{i:000}",
                    12, x, y, 80, ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF));
            }
        }

        int total = nRects + nPaths + nShadows + nBlurs + nText;
        StressHelpers.DrawCountOverlay(e, $"Mixed ({nRects}r+{nPaths}p+{nShadows}s+{nBlurs}b+{nText}t)", total);
    }
}
