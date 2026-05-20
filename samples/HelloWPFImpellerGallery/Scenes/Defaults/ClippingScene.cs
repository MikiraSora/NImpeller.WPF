using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class ClippingScene : IGalleryScene
{
    public string Name => "Clipping";
    public string? Description => "ClipRect, ClipOval, ClipRoundedRect, ClipPath with Intersect/Difference";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        SceneHelpers.ClearBg(b);

        // The thing being clipped: a rainbow striped rectangle
        void DrawStripes(ImpellerDisplayListBuilder b, ImpellerRect bounds)
        {
            using var p = ImpellerPaint.New()!;
            for (int i = 0; i < 12; i++)
            {
                var (r, g, bb) = SceneHelpers.HsvToRgb(i / 12f, 0.85f, 1.0f);
                p.SetColor(new ImpellerColor { Alpha = 1, Red = r, Green = g, Blue = bb });
                int sliceH = (int)(bounds.Height / 12);
                b.DrawRect(new ImpellerRect((int)bounds.X, (int)bounds.Y + i * sliceH, (int)bounds.Width, sliceH), p);
            }
        }

        // 1) ClipRect intersect
        b.Save();
        b.ClipRect(new ImpellerRect(60, 60, 240, 200), ImpellerClipOperation.kImpellerClipOperationIntersect);
        DrawStripes(b, new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        b.Restore();

        // 2) ClipOval intersect
        b.Save();
        b.ClipOval(new ImpellerRect(340, 60, 240, 200), ImpellerClipOperation.kImpellerClipOperationIntersect);
        DrawStripes(b, new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        b.Restore();

        // 3) ClipRoundedRect intersect
        b.Save();
        b.ClipRoundedRect(new ImpellerRect(620, 60, 240, 200),
            SceneHelpers.UniformRadii(40), ImpellerClipOperation.kImpellerClipOperationIntersect);
        DrawStripes(b, new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        b.Restore();

        // 4) ClipPath (star) intersect
        b.Save();
        using (var pb = ImpellerPathBuilder.New()!)
        {
            const int points = 5;
            var cx = 180f; var cy = 460f;
            var rOuter = 110f; var rInner = 46f;
            for (int i = 0; i <= points * 2; i++)
            {
                var ang = -MathF.PI / 2 + i * MathF.PI / points;
                var r = (i % 2 == 0) ? rOuter : rInner;
                var pt = new ImpellerPoint { X = cx + MathF.Cos(ang) * r, Y = cy + MathF.Sin(ang) * r };
                if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
            }
            pb.Close();
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            b.ClipPath(path, ImpellerClipOperation.kImpellerClipOperationIntersect);
        }
        DrawStripes(b, new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        b.Restore();

        // 5) ClipOval *Difference* — hole punched through the rainbow
        b.Save();
        b.ClipRect(new ImpellerRect(360, 340, 480, 240), ImpellerClipOperation.kImpellerClipOperationIntersect);
        b.ClipOval(new ImpellerRect(520, 380, 160, 160), ImpellerClipOperation.kImpellerClipOperationDifference);
        DrawStripes(b, new ImpellerRect(0, 0, e.PixelWidth, e.PixelHeight));
        b.Restore();
    }
}
