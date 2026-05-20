using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class PathsScene : IGalleryScene
{
    public string Name => "Paths";
    public string? Description => "ImpellerPathBuilder: MoveTo, LineTo, QuadraticCurveTo, CubicCurveTo, Close";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);

        // 1) Polyline triangle (MoveTo + LineTo + Close)
        using (var pb = ImpellerPathBuilder.New()!)
        {
            pb.MoveTo(new ImpellerPoint { X = 100, Y = 280 });
            pb.LineTo(new ImpellerPoint { X = 240, Y = 60 });
            pb.LineTo(new ImpellerPoint { X = 380, Y = 280 });
            pb.Close();
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0x70, 0x70));
            b.DrawPath(path, p);
        }

        // 2) Quadratic curve
        using (var pb = ImpellerPathBuilder.New()!)
        {
            pb.MoveTo(new ImpellerPoint { X = 460, Y = 280 });
            pb.QuadraticCurveTo(new ImpellerPoint { X = 600, Y = 40 }, new ImpellerPoint { X = 740, Y = 280 });
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0x70, 0xE8, 0xA8));
            p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
            p.SetStrokeWidth(6f * e.DpiScaleX);
            b.DrawPath(path, p);
        }

        // 3) Cubic curve (heart-ish shape)
        using (var pb = ImpellerPathBuilder.New()!)
        {
            pb.MoveTo(new ImpellerPoint { X = 200, Y = 500 });
            pb.CubicCurveTo(
                new ImpellerPoint { X = 100, Y = 360 },
                new ImpellerPoint { X = 280, Y = 320 },
                new ImpellerPoint { X = 240, Y = 480 });
            pb.CubicCurveTo(
                new ImpellerPoint { X = 200, Y = 380 },
                new ImpellerPoint { X = 380, Y = 360 },
                new ImpellerPoint { X = 280, Y = 500 });
            pb.Close();
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0x70, 0xC8));
            b.DrawPath(path, p);
        }

        // 4) Star (alternating outer/inner radius — fill type Odd creates the hollow look)
        using (var pb = ImpellerPathBuilder.New()!)
        {
            const int points = 5;
            var cx = 600f; var cy = 460f;
            var rOuter = 100f; var rInner = 42f;
            for (int i = 0; i <= points * 2; i++)
            {
                var ang = -MathF.PI / 2 + i * MathF.PI / points;
                var r = (i % 2 == 0) ? rOuter : rInner;
                var pt = new ImpellerPoint { X = cx + MathF.Cos(ang) * r, Y = cy + MathF.Sin(ang) * r };
                if (i == 0) pb.MoveTo(pt); else pb.LineTo(pt);
            }
            pb.Close();
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeOdd)!;
            using var p = ImpellerPaint.New()!;
            p.SetColor(ImpellerColor.FromRgb(0xE8, 0xE8, 0x70));
            b.DrawPath(path, p);
        }
    }

    private static void ClearBg(ImpellerDisplayListBuilder b)
    {
        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0x14, 0x18, 0x1D));
        b.DrawPaint(p);
    }
}
