using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class SaveLayerScene : IGalleryScene
{
    public string Name => "SaveLayer";
    public string? Description => "SaveLayer applies an opacity / blend to a group of drawings as one unit";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);

        // LEFT: per-paint alpha = 0.5 — each shape is half transparent and overlaps blend visibly
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8));
            using var pb = ImpellerPathBuilder.New()!;
            pb.AddRect(new ImpellerRect(40, 40, 280, 280));
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(1f);
            b.DrawPath(path, p);
        }
        b.Save();
        b.ClipRect(new ImpellerRect(40, 40, 280, 280), ImpellerClipOperation.kImpellerClipOperationIntersect);
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(new ImpellerColor { Alpha = 0.5f, Red = 1, Green = 0.2f, Blue = 0.2f });
            b.DrawOval(new ImpellerRect(80, 90, 120, 120), p);
            p.SetColor(new ImpellerColor { Alpha = 0.5f, Red = 0.2f, Green = 1, Blue = 0.2f });
            b.DrawOval(new ImpellerRect(140, 90, 120, 120), p);
            p.SetColor(new ImpellerColor { Alpha = 0.5f, Red = 0.2f, Green = 0.2f, Blue = 1 });
            b.DrawOval(new ImpellerRect(110, 150, 120, 120), p);
        }
        b.Restore();
        DrawLabel(b, e, "Per-paint alpha=0.5", 60, 340);

        // RIGHT: SaveLayer with alpha=0.5 — all 3 shapes are opaque inside the layer,
        //        then the entire layer is composited at 0.5 alpha
        b.Save();
        using (var layerPaint = ImpellerPaint.New()!)
        {
            layerPaint.SetColor(new ImpellerColor { Alpha = 0.5f, Red = 1, Green = 1, Blue = 1 });
            using var noBackdrop = ImpellerImageFilter.CreateBlurNew(0f, 0f, ImpellerTileMode.kImpellerTileModeClamp)!;
            b.SaveLayer(new ImpellerRect(380, 40, 280, 280), layerPaint, noBackdrop);
        }
        using (var p = ImpellerPaint.New()!)
        {
            p.SetColor(new ImpellerColor { Alpha = 1, Red = 1, Green = 0.2f, Blue = 0.2f });
            b.DrawOval(new ImpellerRect(420, 90, 120, 120), p);
            p.SetColor(new ImpellerColor { Alpha = 1, Red = 0.2f, Green = 1, Blue = 0.2f });
            b.DrawOval(new ImpellerRect(480, 90, 120, 120), p);
            p.SetColor(new ImpellerColor { Alpha = 1, Red = 0.2f, Green = 0.2f, Blue = 1 });
            b.DrawOval(new ImpellerRect(450, 150, 120, 120), p);
        }
        b.Restore(); // pops SaveLayer
        b.Restore();
        DrawLabel(b, e, "SaveLayer with alpha=0.5", 400, 340);
    }

    private static void DrawLabel(ImpellerDisplayListBuilder b, ImpellerRenderEventArgs e, string text, float x, float y)
    {
        if (e.Typography == null) return;
        TextBasicsScene.DrawSimpleText(b, e.Typography, text, 16, x, y, e.PixelWidth - (int)x,
            ImpellerColor.FromRgb(0xE8, 0xE8, 0xE8));
    }
}
