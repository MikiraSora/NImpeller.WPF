using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class DashedLinesScene : IGalleryScene
{
    public string Name => "Dashed Lines";
    public string? Description => "DrawDashedLine with varying on/off lengths and stroke widths";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);

        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8));
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);

        // Different dash patterns
        (float onLen, float offLen, float width, string label)[] patterns =
        {
            (16, 10, 3, "16/10"),
            (32, 12, 5, "32/12"),
            ( 8,  8, 4, "8/8"),
            ( 4,  4, 2, "4/4"),
            (40, 20, 8, "40/20"),
            ( 2, 12, 3, "2/12 dots"),
        };

        for (int i = 0; i < patterns.Length; i++)
        {
            p.SetStrokeWidth(patterns[i].width * e.DpiScaleX);
            int y = 80 + i * 70;
            b.DrawDashedLine(
                new ImpellerPoint { X = 80, Y = y },
                new ImpellerPoint { X = 720, Y = y },
                patterns[i].onLen * e.DpiScaleX,
                patterns[i].offLen * e.DpiScaleX,
                p);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }
}
