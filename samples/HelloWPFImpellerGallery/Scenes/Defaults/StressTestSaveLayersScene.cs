using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StressTestSaveLayersScene : StressTestSceneBase
{
    public override string Name => "[StressTest] SaveLayers";
    public override string? Description => "N SaveLayer + child draws — offscreen allocation pressure";
    public override string ItemLabel => "layers";

    public StressTestSaveLayersScene() : base(initial: 100, step: 25, min: 0, max: 2000) { }

    public override void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x0E, 0x10, 0x14);

        var rng = StressHelpers.Seeded(10);
        float t = (float)e.TotalTime.TotalSeconds;
        int count = ItemCount;

        for (int i = 0; i < count; i++)
        {
            int x = rng.Next(e.PixelWidth - 120);
            int y = rng.Next(e.PixelHeight - 120);
            int sz = 60 + rng.Next(60);

            using var layerPaint = ImpellerPaint.New()!;
            layerPaint.SetColor(new ImpellerColor { Alpha = 0.6f, Red = 1, Green = 1, Blue = 1 });
            using var noBackdrop = ImpellerImageFilter.CreateBlurNew(0f, 0f, ImpellerTileMode.kImpellerTileModeClamp)!;
            b.SaveLayer(new ImpellerRect(x, y, sz, sz), layerPaint, noBackdrop);

            using var inner = ImpellerPaint.New()!;
            float hue = (i * 0.01f + t * 0.05f) % 1f;
            var (rr, gg, bb) = SceneHelpers.HsvToRgb(hue, 0.8f, 1.0f);
            inner.SetColor(new ImpellerColor { Alpha = 1, Red = rr, Green = gg, Blue = bb });
            b.DrawOval(new ImpellerRect(x, y, sz, sz), inner);
            inner.SetColor(new ImpellerColor { Alpha = 1, Red = 1 - rr, Green = 1 - gg, Blue = 1 - bb });
            b.DrawOval(new ImpellerRect(x + sz / 3, y + sz / 3, sz / 2, sz / 2), inner);

            b.Restore();
        }

        StressHelpers.DrawCountOverlay(e, "SaveLayers", count);
    }
}
