using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class MaskBlurScene : IGalleryScene
{
    public string Name => "Mask Filter (Blur)";
    public string? Description => "ImpellerMaskFilter.CreateBlurNew with Normal/Solid/Outer/Inner styles";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);

        (ImpellerBlurStyle style, string label)[] styles =
        {
            (ImpellerBlurStyle.kImpellerBlurStyleNormal, "Normal"),
            (ImpellerBlurStyle.kImpellerBlurStyleSolid,  "Solid"),
            (ImpellerBlurStyle.kImpellerBlurStyleOuter,  "Outer"),
            (ImpellerBlurStyle.kImpellerBlurStyleInner,  "Inner"),
        };

        const int cellW = 220;
        const int cellH = 220;
        const int xPad = 30;
        const int yPad = 60;

        for (int i = 0; i < styles.Length; i++)
        {
            int x = xPad + i * (cellW + 10);
            using var mask = ImpellerMaskFilter.CreateBlurNew(styles[i].style, 12f)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0x8F, 0x6F));
            p.SetMaskFilter(mask);
            b.DrawOval(new ImpellerRect(x + 30, yPad + 30, cellW - 60, cellH - 60), p);

            if (e.Typography != null)
                TextBasicsScene.DrawSimpleText(b, e.Typography, styles[i].label, 16 * e.DpiScaleX,
                    x, yPad + cellH + 10, cellW, ImpellerColor.FromRgb(0xCC, 0xCC, 0xCC),
                    align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }

        // Comparison row: varying sigma
        for (int i = 0; i < 6; i++)
        {
            float sigma = 1 + i * 6f;
            using var mask = ImpellerMaskFilter.CreateBlurNew(ImpellerBlurStyle.kImpellerBlurStyleNormal, sigma)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0x8F, 0xC8, 0xE8));
            p.SetMaskFilter(mask);
            int x = 60 + i * 130;
            b.DrawRect(new ImpellerRect(x, 440, 90, 90), p);

            if (e.Typography != null)
                TextBasicsScene.DrawSimpleText(b, e.Typography, $"σ={sigma:0.#}", 12 * e.DpiScaleX,
                    x, 545, 90, ImpellerColor.FromRgb(0xAA, 0xAA, 0xAA),
                    align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }
}
