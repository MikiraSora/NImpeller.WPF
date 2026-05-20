using System;

namespace NImpeller.Wpf;

/// <summary>
/// Snapshot of the Impeller + Vulkan + GPU environment the library is running on.
/// Returned by <see cref="ImpellerSystemInfo.GpuInfo"/> after the first
/// <see cref="ImpellerView.InitializeRender()"/> has initialized the shared context.
/// </summary>
public sealed class ImpellerGpuInfo
{
    /// <summary>Raw Impeller standalone API version (encoded).</summary>
    public uint ImpellerApiVersionRaw { get; init; }
    /// <summary>Decoded Impeller version, e.g. "1.2.0".</summary>
    public string ImpellerApiVersion { get; init; } = "";

    /// <summary>Raw Vulkan API version reported by the physical device (encoded).</summary>
    public uint VulkanApiVersionRaw { get; init; }
    /// <summary>Decoded Vulkan API version, e.g. "1.3.236".</summary>
    public string VulkanApiVersion { get; init; } = "";

    /// <summary>Raw Vulkan driver version (vendor-specific encoding).</summary>
    public uint DriverVersionRaw { get; init; }

    public uint VendorId { get; init; }
    public string VendorName { get; init; } = "";
    public uint DeviceId { get; init; }
    public string DeviceName { get; init; } = "";

    /// <summary>One of Integrated / Discrete / Virtual / Cpu / Other.</summary>
    public string DeviceType { get; init; } = "";

    /// <summary>D3D adapter LUID Impeller's chosen physical device was matched against (host endian, 64-bit packed).</summary>
    public ulong AdapterLuid { get; init; }

    public uint QueueFamilyIndex { get; init; }
    public uint QueueIndex { get; init; }

    /// <summary>Sum of DeviceLocal heap sizes (bytes).</summary>
    public ulong DeviceLocalMemoryBytes { get; init; }
    /// <summary>Sum of HostVisible heap sizes (bytes).</summary>
    public ulong HostVisibleMemoryBytes { get; init; }

    public uint MaxImageDimension2D { get; init; }
    public uint MaxFramebufferWidth { get; init; }
    public uint MaxFramebufferHeight { get; init; }

    // Raw Vulkan handles (informational only — do not free).
    public IntPtr VkInstance { get; init; }
    public IntPtr VkPhysicalDevice { get; init; }
    public IntPtr VkDevice { get; init; }
    public IntPtr VkQueue { get; init; }
}
