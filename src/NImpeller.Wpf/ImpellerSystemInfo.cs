namespace NImpeller.Wpf;

/// <summary>
/// Process-wide diagnostic accessors. Populated lazily when the first
/// <see cref="ImpellerView"/> initializes the shared Impeller context.
/// </summary>
public static class ImpellerSystemInfo
{
    /// <summary>
    /// Snapshot of Impeller / Vulkan / GPU info for the shared context, or null
    /// if no <see cref="ImpellerView"/> has initialized rendering yet.
    /// </summary>
    public static ImpellerGpuInfo? GpuInfo => Interop.ImpellerSharedHost.CachedGpuInfo;
}
