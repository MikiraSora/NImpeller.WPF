using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class TextStylesScene : IGalleryScene
{
    public string Name => "Text Alignment & Decoration";
    public string? Description => "Left / Center / Right alignment, underline, strikethrough";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);
        if (e.Typography == null) return;

        float y = 30 * e.DpiScaleY;
        float lineH = 36 * e.DpiScaleY;

        TextBasicsScene.DrawSimpleText(b, e.Typography, "Left aligned", 22 * e.DpiScaleX,
            40 * e.DpiScaleX, y, e.PixelWidth - 80, ImpellerColor.FromRgb(255, 255, 255),
            align: ImpellerTextAlignment.kImpellerTextAlignmentLeft);
        y += lineH;

        TextBasicsScene.DrawSimpleText(b, e.Typography, "Center aligned", 22 * e.DpiScaleX,
            40 * e.DpiScaleX, y, e.PixelWidth - 80, ImpellerColor.FromRgb(255, 255, 255),
            align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        y += lineH;

        TextBasicsScene.DrawSimpleText(b, e.Typography, "Right aligned", 22 * e.DpiScaleX,
            40 * e.DpiScaleX, y, e.PixelWidth - 80, ImpellerColor.FromRgb(255, 255, 255),
            align: ImpellerTextAlignment.kImpellerTextAlignmentRight);
        y += lineH * 2;

        // Decorations
        DrawTextWithDecoration(b, e.Typography, "Underlined text", 26 * e.DpiScaleX, 40 * e.DpiScaleX, y, e.PixelWidth - 80,
            ImpellerColor.FromRgb(255, 255, 255),
            new ImpellerTextDecoration(
                ImpellerTextDecorationType.kImpellerTextDecorationTypeUnderline,
                ImpellerTextDecorationStyle.kImpellerTextDecorationStyleSolid,
                ImpellerColor.FromRgb(255, 80, 80)));
        y += lineH * 1.4f;

        DrawTextWithDecoration(b, e.Typography, "Strikethrough text", 26 * e.DpiScaleX, 40 * e.DpiScaleX, y, e.PixelWidth - 80,
            ImpellerColor.FromRgb(255, 255, 255),
            new ImpellerTextDecoration(
                ImpellerTextDecorationType.kImpellerTextDecorationTypeLineThrough,
                ImpellerTextDecorationStyle.kImpellerTextDecorationStyleSolid,
                ImpellerColor.FromRgb(80, 200, 255)));
        y += lineH * 1.4f;

        DrawTextWithDecoration(b, e.Typography, "Underline + Overline + dashed", 26 * e.DpiScaleX, 40 * e.DpiScaleX, y, e.PixelWidth - 80,
            ImpellerColor.FromRgb(220, 220, 220),
            new ImpellerTextDecoration(
                ImpellerTextDecorationType.kImpellerTextDecorationTypeUnderline | ImpellerTextDecorationType.kImpellerTextDecorationTypeOverline,
                ImpellerTextDecorationStyle.kImpellerTextDecorationStyleDashed,
                ImpellerColor.FromRgb(160, 220, 80)));
        y += lineH * 1.6f;

        // CJK rendering
        TextBasicsScene.DrawSimpleText(b, e.Typography, "中文字符渲染：你好，世界！", 26 * e.DpiScaleX,
            40 * e.DpiScaleX, y, e.PixelWidth - 80, ImpellerColor.FromRgb(255, 200, 100));
    }

    private static void DrawTextWithDecoration(
        ImpellerDisplayListBuilder b, ImpellerTypographyContext typography,
        string text, float fontSize, float x, float y, int width, ImpellerColor color, ImpellerTextDecoration decoration)
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
        style.SetHeight(1.0f);
        style.SetTextDecoration(decoration);
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
