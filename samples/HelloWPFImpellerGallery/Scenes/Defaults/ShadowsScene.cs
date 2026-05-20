using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class ShadowsScene : IGalleryScene
{
    public string Name => "Shadows";
    public string? Description => "DrawShadow with varying elevation";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);

        var elevations = new float[] { 2, 6, 16, 36 };

        using var fill = ImpellerPaint.New()!;
        fill.SetColor(ImpellerColor.FromRgb(0xF2, 0xF2, 0xF2));
        var shadowColor = ImpellerColor.FromRgb(0x00, 0x00, 0x00);

        for (int i = 0; i < elevations.Length; i++)
        {
            float x = 60 + i * 180;
            float y = 220;

            using var pb = ImpellerPathBuilder.New()!;
            var corner = new ImpellerPoint { X = 24, Y = 24 };
            var radii = new ImpellerRoundingRadii
            {
                Top_left = corner, Top_right = corner,
                Bottom_left = corner, Bottom_right = corner,
            };
            pb.AddRoundedRect(new ImpellerRect((int)x, (int)y, 140, 200), radii);
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;

            b.DrawShadow(path, shadowColor, elevations[i], 0, (float)e.DpiScaleX);
            b.DrawPath(path, fill);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x30, 0x32, 0x38));
        b.DrawPaint(p);
    }
}
