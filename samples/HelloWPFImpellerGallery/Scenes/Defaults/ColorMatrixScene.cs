using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class ColorMatrixScene : IGalleryScene
{
    public string Name => "Color Matrix Filter";
    public string? Description => "ImpellerColorFilter.CreateColorMatrixNew — grayscale, invert, sepia, hue";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);

        // Draw a colorful test pattern, then re-draw it 4 more times in clipped
        // regions, each with a different color matrix.
        (string label, float[] m)[] filters =
        {
            ("Identity", new float[] {
                1,0,0,0,0,
                0,1,0,0,0,
                0,0,1,0,0,
                0,0,0,1,0,
            }),
            ("Grayscale", new float[] {
                0.299f,0.587f,0.114f,0,0,
                0.299f,0.587f,0.114f,0,0,
                0.299f,0.587f,0.114f,0,0,
                0,0,0,1,0,
            }),
            ("Invert", new float[] {
                -1,0,0,0,1,
                0,-1,0,0,1,
                0,0,-1,0,1,
                0,0,0,1,0,
            }),
            ("Sepia", new float[] {
                0.393f,0.769f,0.189f,0,0,
                0.349f,0.686f,0.168f,0,0,
                0.272f,0.534f,0.131f,0,0,
                0,0,0,1,0,
            }),
        };

        const int cellW = 240;
        const int cellH = 180;
        const int xPad = 30;
        const int yPad = 40;

        for (int i = 0; i < filters.Length; i++)
        {
            int col = i % 4;
            int x = xPad + col * (cellW + 8);
            int y = yPad;

            b.Save();
            b.ClipRect(new ImpellerRect(x, y, cellW, cellH), ImpellerClipOperation.kImpellerClipOperationIntersect);

            using var layerPaint = ImpellerPaint.New()!;
            unsafe
            {
                var cm = new ImpellerColorMatrix();
                for (int k = 0; k < 20; k++) cm.m[k] = filters[i].m[k];
                using var cf = ImpellerColorFilter.CreateColorMatrixNew(cm)!;
                layerPaint.SetColorFilter(cf);
            }
            // Use a SaveLayer so the color filter applies to the *entire group* of drawings below.
            using var nullBackdrop = ImpellerImageFilter.CreateBlurNew(0f, 0f, ImpellerTileMode.kImpellerTileModeClamp)!;
            b.SaveLayer(new ImpellerRect(x, y, cellW, cellH), layerPaint, nullBackdrop);

            DrawColorfulPattern(b, x, y, cellW, cellH);

            b.Restore(); // SaveLayer
            b.Restore(); // ClipRect

            if (e.Typography != null)
                TextBasicsScene.DrawSimpleText(b, e.Typography, filters[i].label, 16 * e.DpiScaleX,
                    x, y + cellH + 8, cellW, ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8),
                    align: ImpellerTextAlignment.kImpellerTextAlignmentCenter);
        }
    }

    private static void DrawColorfulPattern(ImpellerDisplayListBuilder b, int x, int y, int w, int h)
    {
        using var p = ImpellerPaint.New()!;
        var colors = new (byte r, byte g, byte bb)[]
        {
            (0xE8, 0x6F, 0x6F), (0xE8, 0xCB, 0x6F), (0x6F, 0xC2, 0xE8),
            (0xB8, 0xE8, 0x6F), (0xE8, 0x6F, 0xC8), (0x6F, 0x80, 0xE8),
        };
        for (int i = 0; i < colors.Length; i++)
        {
            var c = colors[i];
            p.SetColor(ImpellerColor.FromRgb(c.r, c.g, c.bb));
            int cellW = w / 3;
            int cellH = h / 2;
            int cx = x + (i % 3) * cellW;
            int cy = y + (i / 3) * cellH;
            b.DrawOval(new ImpellerRect(cx + 6, cy + 6, cellW - 12, cellH - 12), p);
        }
    }
}
