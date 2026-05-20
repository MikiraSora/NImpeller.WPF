using System;
using System.Runtime.InteropServices;

namespace HelloWPFImpeller.Interop;

/// <summary>
/// Loads the real Vulkan loader (vulkan-1.dll) and exposes the bootstrap function
/// vkGetInstanceProcAddr. Everything else is loaded through that entry point.
/// </summary>
internal static unsafe class VulkanLoader
{
    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr LoadLibraryA(string lpFileName);

    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    private static IntPtr _vulkanModule;
    private static IntPtr _vkGetInstanceProcAddr;

    public static delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr> VkGetInstanceProcAddr
        => (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr>)_vkGetInstanceProcAddr;

    public static void EnsureLoaded()
    {
        if (_vkGetInstanceProcAddr != IntPtr.Zero) return;

        _vulkanModule = LoadLibraryA("vulkan-1.dll");
        if (_vulkanModule == IntPtr.Zero)
            throw new InvalidOperationException(
                "Failed to load vulkan-1.dll. Install the Vulkan Runtime (LunarG SDK or vendor driver).");

        _vkGetInstanceProcAddr = GetProcAddress(_vulkanModule, "vkGetInstanceProcAddr");
        if (_vkGetInstanceProcAddr == IntPtr.Zero)
            throw new InvalidOperationException("vulkan-1.dll loaded but vkGetInstanceProcAddr missing.");
    }
}
