using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

internal sealed class DisplayListBuilderNewScene : IGalleryScene
{
    public string Name => "DisplayListBuilder.New";
    public string? Description => "Creates a nested display list with its own cull rect, then draws it into the frame";

    public void Render(ImpellerRenderEventArgs e)
    {
        TextureSceneHelpers.Clear(e.Builder);
        TextureSceneHelpers.DrawTitle(e, "ImpellerDisplayListBuilder.New", "Build an offscreen command list, then reuse it as one draw call.");

        var tileRect = new ImpellerRect(0, 0, 220, 160);
        using var nestedBuilder = ImpellerDisplayListBuilder.New(tileRect);
        if (nestedBuilder == null) return;

        using var bg = ImpellerPaint.New()!;
        bg.SetColor(ImpellerColor.FromRgb(0x27, 0x34, 0x48));
        nestedBuilder.DrawRoundedRect(tileRect, SceneHelpers.UniformRadii(18), bg);

        using var accent = ImpellerPaint.New()!;
        accent.SetColor(ImpellerColor.FromRgb(0x62, 0xD0, 0xA4));
        nestedBuilder.DrawOval(new ImpellerRect(24, 24, 82, 82), accent);
        accent.SetColor(ImpellerColor.FromRgb(0xF3, 0xC9, 0x69));
        nestedBuilder.DrawRect(new ImpellerRect(118, 32, 72, 96), accent);

        using var line = ImpellerPaint.New()!;
        line.SetColor(ImpellerColor.FromRgb(0xF7, 0xF9, 0xFC));
        line.SetDrawStyle(ImpellerDrawStyle.kImpellerDrawStyleStroke);
        line.SetStrokeWidth(4);
        nestedBuilder.DrawLine(new ImpellerPoint { X = 28, Y = 132 }, new ImpellerPoint { X = 192, Y = 132 }, line);

        using var nestedList = nestedBuilder.CreateDisplayListNew();
        if (nestedList == null) return;

        var x0 = 70;
        var y0 = 130;
        for (var row = 0; row < 2; row++)
        {
            for (var col = 0; col < 3; col++)
            {
                e.Builder.Save();
                e.Builder.Translate(x0 + col * 250, y0 + row * 205);
                e.Builder.Rotate((e.FrameNumber + row * 18 + col * 9) % 16 - 8);
                e.Builder.DrawDisplayList(nestedList, 1.0f);
                e.Builder.Restore();
            }
        }
    }
}
