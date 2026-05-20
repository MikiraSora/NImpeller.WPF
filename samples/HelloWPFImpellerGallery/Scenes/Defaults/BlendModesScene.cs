using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class BlendModesScene : IGalleryScene
{
    public string Name => "Blend Modes";
    public string? Description => "ImpellerBlendMode: Multiply, Screen, Overlay, Plus, Difference, Modulate";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);

        (ImpellerBlendMode mode, string label)[] modes =
        {
            (ImpellerBlendMode.kImpellerBlendModeSourceOver, "SourceOver"),
            (ImpellerBlendMode.kImpellerBlendModeMultiply,   "Multiply"),
            (ImpellerBlendMode.kImpellerBlendModeScreen,     "Screen"),
            (ImpellerBlendMode.kImpellerBlendModeOverlay,    "Overlay"),
            (ImpellerBlendMode.kImpellerBlendModePlus,       "Plus"),
            (ImpellerBlendMode.kImpellerBlendModeDifference, "Difference"),
            (ImpellerBlendMode.kImpellerBlendModeColorDodge, "ColorDodge"),
            (ImpellerBlendMode.kImpellerBlendModeColorBurn,  "ColorBurn"),
        };

        const int cols = 4;
        const int cellW = 180, cellH = 200;
        const int xPad = 30, yPad = 30;

        using var p1 = ImpellerPaint.New()!;
        using var p2 = ImpellerPaint.New()!;
        p1.SetColor(ImpellerColor.FromRgb(0xE8, 0x40, 0x40));
        // p2 color set per-cell with blend mode

        for (int i = 0; i < modes.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            float x = xPad + col * (cellW + 10);
            float y = yPad + row * (cellH + 10);

            // bottom red circle
            b.DrawOval(new ImpellerRect((int)x + 20, (int)y + 30, 110, 110), p1);

            // top blue circle with the test blend mode
            p2.SetColor(ImpellerColor.FromRgb(0x40, 0x80, 0xE8));
            p2.SetBlendMode(modes[i].mode);
            b.DrawOval(new ImpellerRect((int)x + 60, (int)y + 60, 110, 110), p2);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x10, 0x10, 0x10));
        b.DrawPaint(p);
    }
}
