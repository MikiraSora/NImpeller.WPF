using System;
using System.Windows;

namespace NImpeller.Wpf;

/// <summary>
/// Configuration for an <see cref="ImpellerView"/>. Pass to <see cref="ImpellerView.Start(ImpellerViewSettings)"/>,
/// or use the parameterless <see cref="ImpellerView.Start()"/> to get defaults.
///
/// <para><b>Lifetime model</b></para>
/// Settings are consumed in two windows:
/// <list type="number">
///   <item><b>First Start in the process</b> — fields that affect the singleton
///     <c>ImpellerContext</c> (currently <see cref="EnableValidation"/>) are locked.
///     Later views inherit these and cannot change them.</item>
///   <item><b>First Start on this view</b> — every other field is read once to
///     build the per-view GPU resources (swapchain, shared texture, ticker).</item>
/// </list>
/// Subsequent <see cref="ImpellerView.Start(ImpellerViewSettings)"/> calls only
/// re-evaluate <see cref="RenderContinuously"/> for the ticker; other fields are
/// stored on the view but do not trigger a GPU rebuild on their own. See
/// <see cref="ImpellerView.Start(ImpellerViewSettings)"/> for the per-field
/// re-Start behavior. To apply locked or first-Start-only changes, detach the
/// view, wait for <c>Unloaded</c>, then re-attach with the new settings.
/// </summary>
public sealed class ImpellerViewSettings
{
    /// <summary>
    /// When true, the view participates in the global <c>CompositionTarget.Rendering</c>
    /// tick and re-renders every WPF frame. When false, the view only renders when
    /// <see cref="ImpellerView.InvalidateRender"/> is called.
    /// <para><b>Lifetime</b>: re-read on every <see cref="ImpellerView.Start(ImpellerViewSettings)"/>
    /// call to decide whether to (re)register the ticker — but only takes effect when
    /// the ticker is currently unregistered (i.e. before the first Start or after
    /// <see cref="ImpellerView.Stop"/>). Flipping this to <c>false</c> on an already
    /// running view does not stop the ticker; call <see cref="ImpellerView.Stop"/>.</para>
    /// </summary>
    public bool RenderContinuously { get; init; } = true;

    /// <summary>
    /// When true, the view's backing texture is allocated in PHYSICAL pixels using the
    /// system DPI, so text and 1-px strokes stay sharp on high-DPI displays.
    /// When false, the texture is allocated in DIPs (logical pixels) — cheaper but
    /// blurry on >100% DPI.
    /// <para><b>Lifetime</b>: applied at first <c>Start</c>. Later changes are stored
    /// but do not rebuild the existing texture on their own; the new value will be
    /// observed on the next resize or DPI change. To apply immediately, detach + re-attach
    /// the view.</para>
    /// </summary>
    public bool UseDeviceDpi { get; init; } = true;

    /// <summary>
    /// Override the layout size returned by <c>MeasureOverride</c>. When null (default),
    /// the view fills the available size of its parent container.
    /// <para><b>Lifetime</b>: read on every <c>MeasureOverride</c>. Call
    /// <c>InvalidateMeasure</c> on the view to apply a new value sooner.</para>
    /// </summary>
    public Size? LogicalSizeOverride { get; init; }

    /// <summary>
    /// Enable Vulkan validation layers.
    /// <para><b>Lifetime</b>: process-wide, locked at the very first <c>Start</c> in
    /// the process — the underlying <c>ImpellerContext</c> / <c>VkInstance</c> is a
    /// singleton shared by every <see cref="ImpellerView"/>. Subsequent values are
    /// silently ignored. To toggle, the entire process must be restarted (or
    /// <c>ImpellerSharedHost.Shutdown</c> called before the next Acquire — but this
    /// disposes the GPU context and is rarely useful in production).</para>
    /// </summary>
    public bool EnableValidation { get; init; } = false;
}
