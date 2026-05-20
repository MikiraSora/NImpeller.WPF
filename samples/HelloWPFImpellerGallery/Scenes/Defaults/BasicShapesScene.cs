using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class BasicShapesScene : IGalleryScene
{
    public string Name => "Basic Shapes";
    public string? Description => "DrawRect, DrawOval, DrawRoundedRect, DrawLine";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBackground(b, 0x1A, 0x1D, 0x22);

        using var paintFill = ImpellerPaint.New()!;

        // Rectangle
        paintFill.SetColor(ImpellerColor.FromRgb(0xE8, 0x6F, 0x6F));
        b.DrawRect(new ImpellerRect(40, 40, 200, 140), paintFill);

        // Oval
        paintFill.SetColor(ImpellerColor.FromRgb(0x6F, 0xC2, 0xE8));
        b.DrawOval(new ImpellerRect(280, 40, 200, 140), paintFill);

        // Rounded rect
        paintFill.SetColor(ImpellerColor.FromRgb(0xE8, 0xCB, 0x6F));
        var corner = new ImpellerPoint { X = 24, Y = 24 };
        var radii = new ImpellerRoundingRadii
        {
            Top_left = corner, Top_right = corner,
            Bottom_left = corner, Bottom_right = corner,
        };
        b.DrawRoundedRect(new ImpellerRect(520, 40, 200, 140), radii, paintFill);

        // Diagonal line
        using var paintLine = ImpellerPaint.New()!;
        paintLine.SetColor(ImpellerColor.FromRgb(0xFF, 0xFF, 0xFF));
        paintLine.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        paintLine.SetStrokeWidth(4f * e.DpiScaleX);
        b.DrawLine(new ImpellerPoint { X = 40, Y = 240 }, new ImpellerPoint { X = 720, Y = 380 }, paintLine);

        // Rounded rect difference (outer minus inner) — like a ring shape
        paintFill.SetColor(ImpellerColor.FromRgb(0xB8, 0xE8, 0x6F));
        var outerR = new ImpellerRect(40, 420, 200, 200);
        var innerR = new ImpellerRect(80, 460, 120, 120);
        b.DrawRoundedRectDifference(outerR, radii, innerR, radii, paintFill);
    }

    private static void ClearBackground(ImpellerDisplayListBuilder b, byte r, byte g, byte bb)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(r, g, bb));
        b.DrawPaint(p);
    }
}
