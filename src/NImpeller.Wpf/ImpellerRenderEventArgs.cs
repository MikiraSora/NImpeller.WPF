using System;

using NImpeller;

namespace NImpeller.Wpf;

/// <summary>
/// Event args delivered to <see cref="ImpellerView.Render"/> handlers. Provides the
/// Impeller drawing primitives + timing/size information for the current frame.
///
/// The <see cref="Builder"/> is pre-created and will be turned into a display list and
/// drawn onto the view's surface after the handler returns; do not store or dispose it.
/// </summary>
public sealed class ImpellerRenderEventArgs : EventArgs
{
    /// <summary>Create render event data for a single Impeller frame.</summary>
    public ImpellerRenderEventArgs(
        ImpellerView source,
        ImpellerDisplayListBuilder builder,
        ImpellerTypographyContext? typography,
        int pixelWidth,
        int pixelHeight,
        float dpiScaleX,
        float dpiScaleY,
        TimeSpan deltaTime,
        TimeSpan totalTime,
        long frameNumber)
        : this(source, builder, null, typography, pixelWidth, pixelHeight, dpiScaleX, dpiScaleY, deltaTime, totalTime, frameNumber)
    {
    }

    /// <summary>Create render event data for a single Impeller frame.</summary>
    public ImpellerRenderEventArgs(
        ImpellerView source,
        ImpellerDisplayListBuilder builder,
        ImpellerContext? context,
        ImpellerTypographyContext? typography,
        int pixelWidth,
        int pixelHeight,
        float dpiScaleX,
        float dpiScaleY,
        TimeSpan deltaTime,
        TimeSpan totalTime,
        long frameNumber)
    {
        Source = source;
        Builder = builder;
        Context = context;
        Typography = typography;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        DpiScaleX = dpiScaleX;
        DpiScaleY = dpiScaleY;
        DeltaTime = deltaTime;
        TotalTime = totalTime;
        FrameNumber = frameNumber;
    }

    /// <summary>The view that is being rendered.</summary>
    public ImpellerView Source { get; }

    /// <summary>The active display-list builder. Issue all <c>Draw*</c> calls on this.</summary>
    public ImpellerDisplayListBuilder Builder { get; }

    /// <summary>The Impeller context used by this view, when supplied by the render host.</summary>
    public ImpellerContext? Context { get; }

    /// <summary>Shared typography context (null if Impeller failed to create one).</summary>
    public ImpellerTypographyContext? Typography { get; }

    /// <summary>Backing render-target width in pixels.</summary>
    public int PixelWidth { get; }

    /// <summary>Backing render-target height in pixels.</summary>
    public int PixelHeight { get; }

    /// <summary>
    /// Horizontal DPI scale used by the view (1.0 at 96 DPI, 1.5 at 144 DPI, etc.).
    /// Multiply font sizes / 1-px strokes by this for crispness.
    /// </summary>
    public float DpiScaleX { get; }

    /// <summary>
    /// Vertical DPI scale used by the view. Equal to <see cref="DpiScaleX"/> on almost
    /// all setups, but separate on virtual / projected displays with
    /// non-square DPI.
    /// </summary>
    public float DpiScaleY { get; }

    /// <summary>Time since the previous frame on this view.</summary>
    public TimeSpan DeltaTime { get; }

    /// <summary>Elapsed render-loop time for this view.</summary>
    public TimeSpan TotalTime { get; }

    /// <summary>Monotonically increasing frame counter (starts at 1 for the first frame).</summary>
    public long FrameNumber { get; }
}
