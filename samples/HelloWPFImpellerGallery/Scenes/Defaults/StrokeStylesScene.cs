using System;

using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;
internal sealed class StrokeStylesScene : IGalleryScene
{
    public string Name => "Stroke Caps & Joins";
    public string? Description => "ImpellerStrokeCap: Butt/Round/Square. ImpellerStrokeJoin: Miter/Round/Bevel";

    public void Render(ImpellerRenderEventArgs e)
    {
        var b = e.Builder;
        ClearBg(b);

        var caps = new[] { ImpellerStrokeCap.kImpellerStrokeCapButt, ImpellerStrokeCap.kImpellerStrokeCapRound, ImpellerStrokeCap.kImpellerStrokeCapSquare };
        var capNames = new[] { "Butt", "Round", "Square" };

        using var p = ImpellerPaint.New()!;
        p.SetColor(ImpellerColor.FromRgb(0xE8, 0xC8, 0x70));
        p.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        p.SetStrokeWidth(28f * e.DpiScaleX);

        for (int i = 0; i < caps.Length; i++)
        {
            p.SetStrokeCap(caps[i]);
            int y = 80 + i * 80;
            b.DrawLine(new ImpellerPoint { X = 120, Y = y }, new ImpellerPoint { X = 520, Y = y }, p);
        }

        // Joins illustrated by V-shaped paths
        var joins = new[] { ImpellerStrokeJoin.kImpellerStrokeJoinMiter, ImpellerStrokeJoin.kImpellerStrokeJoinRound, ImpellerStrokeJoin.kImpellerStrokeJoinBevel };
        for (int i = 0; i < joins.Length; i++)
        {
            using var pb = ImpellerPathBuilder.New()!;
            float x0 = 80 + i * 240;
            pb.MoveTo(new ImpellerPoint { X = x0, Y = 480 });
            pb.LineTo(new ImpellerPoint { X = x0 + 90, Y = 360 });
            pb.LineTo(new ImpellerPoint { X = x0 + 180, Y = 480 });
            using var path = pb.TakePathNew(ImpellerFillType.kImpellerFillTypeNonZero)!;

            p.SetStrokeJoin(joins[i]);
            p.SetStrokeCap(ImpellerStrokeCap.kImpellerStrokeCapButt);
            p.SetColor(ImpellerColor.FromRgb(0x70, 0xC8, 0xE8));
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
