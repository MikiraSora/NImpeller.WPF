using System;

namespace NImpeller.Wpf;

/// <summary>
/// Thrown when an <see cref="ImpellerView"/> fails to (re)build or render its
/// GPU resources. The <see cref="Exception.InnerException"/> identifies the
/// underlying cause (Vulkan, D3D, allocation, swapchain out-of-date, ...).
///
/// Currently raised from:
/// <list type="bullet">
///   <item>The dispatcher-tick resize path, when resource resize fails and leaves
///         the view without a usable swapchain.</item>
///   <item>The per-monitor DPI rebuild path, when D3DImage or swapchain resources
///         cannot be rebuilt for the new physical size.</item>
///   <item><c>ImpellerSharedHost.AcquireAndStart</c>, when the Vulkan physical
///         device list does not contain a device matching the D3D adapter LUID
///         (shared-texture interop would silently corrupt).</item>
/// </list>
/// </summary>
public sealed class ImpellerRenderErrorException : Exception
{
    /// <summary>Create a render error with a diagnostic message.</summary>
    public ImpellerRenderErrorException(string message)
        : base(message)
    {
    }

    /// <summary>Create a render error with a diagnostic message and the original failure.</summary>
    public ImpellerRenderErrorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
