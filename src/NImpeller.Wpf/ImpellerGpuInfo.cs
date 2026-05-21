using System;

namespace NImpeller.Wpf;

/// <summary>
/// Snapshot of the Impeller + Vulkan + GPU environment the library is running on.
/// Returned by <see cref="ImpellerSystemInfo.GpuInfo"/> after the first
/// <see cref="ImpellerView"/> has initialized the shared context.
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

    /// <summary>PCI vendor identifier reported by Vulkan.</summary>
    public uint VendorId { get; init; }
    /// <summary>Human-readable vendor name when known; otherwise the hexadecimal vendor id.</summary>
    public string VendorName { get; init; } = "";
    /// <summary>Vendor-specific device identifier reported by Vulkan.</summary>
    public uint DeviceId { get; init; }
    /// <summary>Physical device name reported by the Vulkan driver.</summary>
    public string DeviceName { get; init; } = "";

    /// <summary>One of Integrated / Discrete / Virtual / Cpu / Other.</summary>
    public string DeviceType { get; init; } = "";

    /// <summary>D3D adapter LUID Impeller's chosen physical device was matched against (host endian, 64-bit packed).</summary>
    public ulong AdapterLuid { get; init; }

    /// <summary>Graphics queue family index used by Impeller.</summary>
    public uint QueueFamilyIndex { get; init; }
    /// <summary>Queue index within <see cref="QueueFamilyIndex"/> used by Impeller.</summary>
    public uint QueueIndex { get; init; }

    /// <summary>Sum of DeviceLocal heap sizes (bytes).</summary>
    public ulong DeviceLocalMemoryBytes { get; init; }
    /// <summary>Sum of HostVisible heap sizes (bytes).</summary>
    public ulong HostVisibleMemoryBytes { get; init; }

    /// <summary>Maximum 2D image dimension supported by the selected Vulkan device.</summary>
    public uint MaxImageDimension2D { get; init; }
    /// <summary>Maximum framebuffer width supported by the selected Vulkan device.</summary>
    public uint MaxFramebufferWidth { get; init; }
    /// <summary>Maximum framebuffer height supported by the selected Vulkan device.</summary>
    public uint MaxFramebufferHeight { get; init; }

    // Raw Vulkan handles (informational only — do not free).
    /// <summary>Raw Vulkan instance handle for diagnostics only; callers must not free it.</summary>
    public IntPtr VkInstance { get; init; }
    /// <summary>Raw Vulkan physical device handle for diagnostics only; callers must not free it.</summary>
    public IntPtr VkPhysicalDevice { get; init; }
    /// <summary>Raw Vulkan logical device handle for diagnostics only; callers must not free it.</summary>
    public IntPtr VkDevice { get; init; }
    /// <summary>Raw Vulkan queue handle for diagnostics only; callers must not free it.</summary>
    public IntPtr VkQueue { get; init; }
}
