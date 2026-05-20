using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class TextBasicsScene : IGalleryScene
{
    public string Name => "Text Basics";
    public string? Description => "Font size & weight via ImpellerParagraphBuilder + ImpellerParagraphStyle";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);
        if (e.Typography == null) return;

        var sizes = new (float size, string label)[]
        {
            (12 * e.DpiScaleX, "12pt"), (16 * e.DpiScaleX, "16pt"), (22 * e.DpiScaleX, "22pt"),
            (32 * e.DpiScaleX, "32pt"), (48 * e.DpiScaleX, "48pt"),
        };
        float y = 40 * e.DpiScaleY;
        foreach (var (size, label) in sizes)
        {
            DrawSimpleText(b, e.Typography, $"{label} The quick brown fox", size, x: 40 * e.DpiScaleX, y, e.PixelWidth,
                ImpellerColor.FromRgb(255, 255, 255), ImpellerFontWeight.kImpellerFontWeight400);
            y += size + 12 * e.DpiScaleY;
        }

        y += 24 * e.DpiScaleY;
        var weights = new (ImpellerFontWeight w, string label)[]
        {
            (ImpellerFontWeight.kImpellerFontWeight300, "Light"),
            (ImpellerFontWeight.kImpellerFontWeight400, "Regular"),
            (ImpellerFontWeight.kImpellerFontWeight500, "Medium"),
            (ImpellerFontWeight.kImpellerFontWeight700, "Bold"),
            (ImpellerFontWeight.kImpellerFontWeight900, "Black"),
        };
        foreach (var (w, label) in weights)
        {
            DrawSimpleText(b, e.Typography, $"{label} weight", 24 * e.DpiScaleX, x: 40 * e.DpiScaleX, y, e.PixelWidth,
                ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8), w);
            y += 32 * e.DpiScaleY;
        }
    }

    internal static void DrawSimpleText(
        ImpellerDisplayListBuilder b, ImpellerTypographyContext typography,
        string text, float fontSize, float x, float y, int width, ImpellerColor color,
        ImpellerFontWeight weight = ImpellerFontWeight.kImpellerFontWeight400,
        ImpellerTextAlignment align = ImpellerTextAlignment.kImpellerTextAlignmentLeft)
    {
        using var paragraphBuilder = typography.ParagraphBuilderNew();
        if (paragraphBuilder == null) return;
        using var style = ImpellerParagraphStyle.New();
        if (style == null) return;
        using var paint = ImpellerPaint.New();
        if (paint == null) return;

        paint.SetColor(color);
        style.SetForeground(paint);
        style.SetFontSize(MathF.Round(fontSize));
        style.SetFontWeight(weight);
        style.SetHeight(1.0f);
        style.SetTextAlignment(align);
        paragraphBuilder.PushStyle(style);
        paragraphBuilder.AddText(text);
        using var paragraph = paragraphBuilder.BuildParagraphNew(width: width);
        if (paragraph == null) return;
        b.DrawParagraph(paragraph, new ImpellerPoint { X = MathF.Round(x), Y = MathF.Round(y) });
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }
}
