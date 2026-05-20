using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestCirclesScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Circles";
    public override string? Description => "Draw N small filled ovals per frame";
    public override string ItemLabel => "circles";

    public StressTestCirclesScene() : base(initial: 10000, step: 1000, min: 0, max: 200000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(2);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        using var p = ImpellerPaint.New()!;
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth);
            int y = rng.Next(e.PixelHeight);
            int sz = 4 + rng.Next(10);

            float hue = (i * 0.0007f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.7f, 1.0f);
            p.SetColor(new ImpellerColor { Alpha = 0.7f, Red = rr, Green = gg, Blue = bb });
            b.DrawOval(new ImpellerRect(x, y, sz, sz), p);
        }

        StressHelpers.DrawCountOverlay(e, "Circles", count);
    }
}
