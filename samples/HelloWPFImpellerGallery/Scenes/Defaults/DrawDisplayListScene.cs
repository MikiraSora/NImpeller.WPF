using NImpeller;
using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

internal sealed class DrawDisplayListScene : IGalleryScene
{
    public string Name => "DrawDisplayList";
    public string? Description => "Draws the same immutable display list with different transforms and opacity";

    public void Render(ImpellerRenderEventArgs e)
    {
        TextureSceneHelpers.Clear(e.Builder);
        TextureSceneHelpers.DrawTitle(e, "DrawDisplayList", "One display list is replayed several times with transform and opacity changes.");

        using var reusableBuilder = ImpellerDisplayListBuilder.New(new ImpellerRect(0, 0, 180, 180));
        if (reusableBuilder == null) return;

        using var paint = ImpellerPaint.New()!;
        paint.SetColor(ImpellerColor.FromRgb(0x42, 0x9B, 0xD7));
        reusableBuilder.DrawOval(new ImpellerRect(18, 18, 144, 144), paint);

        paint.SetColor(ImpellerColor.FromRgb(0xF0, 0x66, 0x66));
        reusableBuilder.DrawRoundedRect(new ImpellerRect(60, 36, 84, 108), SceneHelpers.UniformRadii(16), paint);

        paint.SetColor(ImpellerColor.FromRgb(0xF8, 0xD9, 0x76));
        reusableBuilder.DrawRect(new ImpellerRect(36, 118, 116, 24), paint);

        using var reusableList = reusableBuilder.CreateDisplayListNew();
        if (reusableList == null) return;

        var centerX = e.PixelWidth * 0.5f;
        var centerY = e.PixelHeight * 0.55f;
        for (var i = 0; i < 8; i++)
        {
            var angle = i * 45f + (float)e.TotalTime.TotalSeconds * 18f;
            var opacity = 0.35f + i * 0.08f;
            e.Builder.Save();
            e.Builder.Translate(centerX, centerY);
            e.Builder.Rotate(angle);
            e.Builder.Translate(155, -90);
            e.Builder.Scale(0.75f + i * 0.045f, 0.75f + i * 0.045f);
            e.Builder.DrawDisplayList(reusableList, opacity);
            e.Builder.Restore();
        }
    }
}
