using System;
using System.Windows;

namespace NImpeller.Wpf;

/// <summary>
/// Configuration for an <see cref="ImpellerView"/>. Pass to <see cref="ImpellerView.Start(ImpellerViewSettings)"/>
/// or use the parameterless <see cref="ImpellerView.Start()"/> to get defaults.
///
/// Note: a few fields only take effect when the <b>first</b> view in the process is
/// started (e.g. <see cref="EnableValidation"/>), because the underlying
/// <c>ImpellerContext</c> is a per-process singleton shared across all views.
/// </summary>
public sealed class ImpellerViewSettings
{
    /// <summary>
    /// When true, the view participates in the global <c>CompositionTarget.Rendering</c>
    /// tick and re-renders every WPF frame. When false, the view only renders when
    /// <see cref="ImpellerView.InvalidateRender"/> is called.
    /// </summary>
    public bool RenderContinuously { get; init; } = true;

    /// <summary>
    /// When true, the view's backing texture is allocated in PHYSICAL pixels using the
    /// system DPI, so text and 1-px strokes stay sharp on high-DPI displays.
    /// When false, the texture is allocated in DIPs (logical pixels) — cheaper but
    /// blurry on >100% DPI.
    /// </summary>
    public bool UseDeviceDpi { get; init; } = true;

    /// <summary>
    /// Override the layout size returned by <c>MeasureOverride</c>. When null (default),
    /// the view fills the available size of its parent container.
    /// </summary>
    public Size? LogicalSizeOverride { get; init; }

    /// <summary>
    /// Enable Vulkan validation layers. Only honored on the FIRST view that starts in
    /// the process; subsequent views ignore this flag because the underlying
    /// <c>ImpellerContext</c> / <c>VkInstance</c> is shared.
    /// </summary>
    public bool EnableValidation { get; init; } = false;
}
