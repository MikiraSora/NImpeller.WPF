using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class CardLayoutScene : IGalleryScene
{
    public string Name => "Card Layout (UI Mockup)";
    public string? Description => "Material-style cards: shadow + rounded rect + multi-line text + accent bar";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b, 0x1F, 0x22, 0x29);

        var cards = new (string title, string body, byte r, byte g, byte bb)[]
        {
            ("Performance", "GPU-accelerated 2D vector rendering powered by Vulkan, with consistent frame pacing.", 0x6F, 0xC2, 0xE8),
            ("Cross-platform", "The Impeller engine targets Windows, macOS, Linux, iOS, and Android from one codebase.", 0xB8, 0xE8, 0x6F),
            ("Modern APIs", "Display lists, color filters, image filters, blend modes and full typography support.", 0xE8, 0xCB, 0x6F),
            ("Native interop", "Integrates with WPF via D3DImage thanks to VK_KHR_external_memory_win32.", 0xE8, 0x6F, 0xC8),
        };

        const int cols = 2;
        const int padOuter = 30;
        const int gap = 24;
        int cellW = (e.PixelWidth - padOuter * 2 - gap * (cols - 1)) / cols;
        int cellH = (e.PixelHeight - padOuter * 2 - gap) / 2;

        for (int i = 0; i < cards.Length; i++)
        {
            int col = i % cols;
            int row = i / cols;
            int x = padOuter + col * (cellW + gap);
            int y = padOuter + row * (cellH + gap);

            // Drop shadow
            using (var pb = ImpellerPathBuilder.New()!)
            {
                pb.AddRoundedRect(new ImpellerRect(x, y, cellW, cellH), SceneHelpers.UniformRadii(14));
                using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
                b.DrawShadow(path, ImpellerColor.FromRgb(0x00, 0x00, 0x00), 8f, 0, (float)e.DpiScaleX);
            }

            // Card background
            using (var p = ImpellerPaint.New()!)
            {
                p.SetColor(ImpellerColor.FromRgb(0x2D, 0x32, 0x3C));
                b.DrawRoundedRect(new ImpellerRect(x, y, cellW, cellH), SceneHelpers.UniformRadii(14), p);
            }

            // Accent strip (left)
            using (var p = ImpellerPaint.New()!)
            {
                p.SetColor(ImpellerColor.FromRgb(cards[i].r, cards[i].g, cards[i].bb));
                b.Save();
                b.ClipRoundedRect(new ImpellerRect(x, y, cellW, cellH), SceneHelpers.UniformRadii(14),
                    ImpellerClipOperation.kImpellerClipOperationIntersect);
                b.DrawRect(new ImpellerRect(x, y, 6, cellH), p);
                b.Restore();
            }

            if (e.Typography != null)
            {
                int tx = x + 24;
                int tw = cellW - 48;
                TextBasicsScene.DrawSimpleText(b, e.Typography, cards[i].title, 22 * e.DpiScaleX,
                    tx, y + 22 * e.DpiScaleY, tw,
                    ImpellerColor.FromRgb(cards[i].r, cards[i].g, cards[i].bb),
                    weight: ImpellerFontWeight.kImpellerFontWeight700);

                DrawWrappedBody(b, e.Typography, cards[i].body, 14 * e.DpiScaleY, tx, y + 64 * e.DpiScaleY, tw,
                    ImpellerColor.FromRgb(0xC8, 0xCD, 0xD5));
            }
        }
    }

    private static void DrawWrappedBody(ImpellerDisplayListBuilder b, ImpellerTypographyContext typography,
        string text, float fontSize, float x, float y, int width, ImpellerColor color)
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
        style.SetHeight(1.35f);
        paragraphBuilder.PushStyle(style);
        paragraphBuilder.AddText(text);
        using var paragraph = paragraphBuilder.BuildParagraphNew(width: width);
        if (paragraph == null) return;
        b.DrawParagraph(paragraph, new ImpellerPoint { X = MathF.Round(x), Y = MathF.Round(y) });
    }
}
