using NImpeller.Wpf;

namespace HelloWPFImpellerGallery.Scenes;

/// <summary>
/// A renderable demo scene for the gallery. Each scene focuses on a particular
/// aspect of <c>ImpellerDisplayListBuilder</c> / <c>ImpellerPaint</c> / etc.
/// </summary>
public interface IGalleryScene
{
    /// <summary>Display name in the gallery list.</summary>
    string Name { get; }

    /// <summary>Optional one-line description shown beneath the name.</summary>
    string? Description { get; }

    /// <summary>
    /// Render one frame. The scene should treat the entire <c>e.PixelWidth</c> x
    /// <c>e.PixelHeight</c> area as its canvas, and may use <c>e.TotalTime</c> /
    /// <c>e.FrameNumber</c> for animation.
    /// </summary>
    void Render(ImpellerRenderEventArgs e);
}
