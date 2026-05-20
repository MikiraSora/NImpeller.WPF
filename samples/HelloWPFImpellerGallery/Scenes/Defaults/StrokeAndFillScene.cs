using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StrokeAndFillScene : IGalleryScene
{
    public string Name => "Stroke vs Fill";
    public string? Description => "ImpellerDrawStyle Fill / Stroke / StrokeAndFill, varying width";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);

        using var fill = ImpellerPaint.New()!;
        fill.SetColor(ImpellerColor.FromRgb(0x70, 0xA8, 0xE8));
        fill.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleFill);
        b.DrawRect(new ImpellerRect(40, 40, 180, 180), fill);

        using var stroke = ImpellerPaint.New()!;
        stroke.SetColor(ImpellerColor.FromRgb(0xE8, 0xA8, 0x70));
        stroke.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        stroke.SetStrokeWidth(6f * e.DpiScaleX);
        b.DrawRect(new ImpellerRect(260, 40, 180, 180), stroke);

        using var both = ImpellerPaint.New()!;
        both.SetColor(ImpellerColor.FromRgb(0xE8, 0xE8, 0x70));
        both.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStrokeAndFill);
        both.SetStrokeWidth(10f * e.DpiScaleX);
        b.DrawRect(new ImpellerRect(480, 40, 180, 180), both);

        // Increasing stroke widths
        using var label = ImpellerPaint.New()!;
        label.SetColor(ImpellerColor.FromRgb(0xCC, 0xCC, 0xCC));
        label.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        for (int i = 0; i < 8; i++)
        {
            label.SetStrokeWidth((1 + i * 2) * e.DpiScaleX);
            int y = 280 + i * 38;
            b.DrawLine(new ImpellerPoint { X = 40, Y = y }, new ImpellerPoint { X = 700, Y = y }, label);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }
}
