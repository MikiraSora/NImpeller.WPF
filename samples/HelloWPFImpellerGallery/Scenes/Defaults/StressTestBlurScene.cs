using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestBlurScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Blurred Shapes";
    public override string? Description => "N ovals with ImpellerMaskFilter blur — bandwidth-bound, very heavy";
    public override string ItemLabel => "blurred ovals";

    public StressTestBlurScene() : base(initial: 200, step: 50, min: 0, max: 5000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(8);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth);
            int y = rng.Next(e.PixelHeight);
            int sz = 30 + rng.Next(40);
            float sigma = 4 + rng.Next(10);

            float hue = (i * 0.005f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.8f, 1.0f);

            using var mask = ImpellerMaskFilter.CreateBlurNew(ImpellerBlurStyle.kImpellerBlurStyleNormal, sigma)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(new ImpellerColor { Alpha = 0.85f, Red = rr, Green = gg, Blue = bb });
            p.SetMaskFilter(mask);
            b.DrawOval(new ImpellerRect(x, y, sz, sz), p);
        }

        StressHelpers.DrawCountOverlay(e, "Blurred shapes", count);
    }
}
