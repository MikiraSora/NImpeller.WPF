using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestShadowsScene : StressTestSceneBase
{
    public override string Name => "[StressTest] Shadows";
    public override string? Description => "N DrawShadow calls + filled cards — typical UI card grid at scale";
    public override string ItemLabel => "shadows";

    public StressTestShadowsScene() : base(initial: 200, step: 50, min: 0, max: 5000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x18, 0x1C, 0x24);

        var rng = StressHelpers.Seeded(9);
        var shadowColor = ImpellerColor.FromRgb(0x00, 0x00, 0x00);
        var radii = SceneHelpers.UniformRadii(8);
        int count = ItemCount;

        using var fill = ImpellerPaint.New()!;
        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth - 80);
            int y = rng.Next(e.PixelHeight - 60);
            int w = 40 + rng.Next(60);
            int h = 30 + rng.Next(40);
            float elev = 2 + rng.Next(10);

            using (var pb = ImpellerPathBuilder.New()!)
            {
                pb.AddRoundedRect(new ImpellerRect(x, y, w, h), radii);
                using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                b.DrawShadow(path, shadowColor, elev, 0, (float)e.DpiScaleX);
            }

            byte r = (byte)(160 + rng.Next(96));
            byte g = (byte)(160 + rng.Next(96));
            byte bb = (byte)(160 + rng.Next(96));
            fill.SetColor(ImpellerColor.FromRgb(r, g, bb));
            b.DrawRoundedRect(new ImpellerRect(x, y, w, h), radii, fill);
        }

        StressHelpers.DrawCountOverlay(e, "Shadowed cards", count);
    }
}
