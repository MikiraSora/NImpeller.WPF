using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Silk.NET.Vulkan;

namespace NImpeller.Wpf.Interop;

/// <summary>
/// Vulkan proc-address interceptor. Sits between Impeller and the real Vulkan loader.
///
/// For each "hookable" entry point Impeller requests, we
///   1. Resolve the real function via the system vulkan-1.dll loader
///   2. Cache that real pointer into a VkTrampolines static field
///   3. Return our own trampoline's function pointer to Impeller
/// Everything else passes through untouched.
/// </summary>
internal static unsafe class VkProcInterceptor
{
    private static readonly HashSet<string> HookedFunctions = new(StringComparer.Ordinal);

    public static void Initialize() => VulkanLoader.EnsureLoaded();

    /// <summary>The callback handed to ImpellerContext.CreateVulkanNew.</summary>
    public static IntPtr GetProcAddress(IntPtr vkInstance, IntPtr procName)
    {
        if (procName == IntPtr.Zero) return IntPtr.Zero;

        var realPtr = VulkanLoader.VkGetInstanceProcAddr(vkInstance, procName);
        var name = Marshal.PtrToStringAnsi(procName);
        if (name == null) return realPtr;

        switch (name)
        {
            case "vkCreateInstance":
                VkTrampolines.RealCreateInstance =
                    (delegate* unmanaged[Cdecl]<InstanceCreateInfo*, AllocationCallbacks*, Instance*, Result>)realPtr;
                return MarkHooked(name, (IntPtr)(delegate* unmanaged[Cdecl]<InstanceCreateInfo*, AllocationCallbacks*, Instance*, Result>)&VkTrampolines.CreateInstance);

            case "vkCreateDevice":
                VkTrampolines.RealCreateDevice =
                    (delegate* unmanaged[Cdecl]<PhysicalDevice, DeviceCreateInfo*, AllocationCallbacks*, Device*, Result>)realPtr;
                return MarkHooked(name, (IntPtr)(delegate* unmanaged[Cdecl]<PhysicalDevice, DeviceCreateInfo*, AllocationCallbacks*, Device*, Result>)&VkTrampolines.CreateDevice);

            case "vkEnumeratePhysicalDevices":
                VkTrampolines.RealEnumeratePhysicalDevices =
                    (delegate* unmanaged[Cdecl]<Instance, uint*, PhysicalDevice*, Result>)realPtr;
                EnsureRealGetPhysicalDeviceProperties2(vkInstance);
                return MarkHooked(name, (IntPtr)(delegate* unmanaged[Cdecl]<Instance, uint*, PhysicalDevice*, Result>)&VkTrampolines.EnumeratePhysicalDevices);

            case "vkGetPhysicalDeviceProperties2":
            case "vkGetPhysicalDeviceProperties2KHR":
                VkTrampolines.RealGetPhysicalDeviceProperties2 =
                    (delegate* unmanaged[Cdecl]<PhysicalDevice, PhysicalDeviceProperties2*, void>)realPtr;
                return realPtr;

            case "vkGetPhysicalDeviceSurfaceCapabilitiesKHR":
                VkTrampolines.RealGetPhysicalDeviceSurfaceCapabilitiesKHR =
                    (delegate* unmanaged[Cdecl]<PhysicalDevice, SurfaceKHR, SurfaceCapabilitiesKHR*, Result>)realPtr;
                return MarkHooked(name, (IntPtr)(delegate* unmanaged[Cdecl]<PhysicalDevice, SurfaceKHR, SurfaceCapabilitiesKHR*, Result>)&VkTrampolines.GetPhysicalDeviceSurfaceCapabilitiesKHR);

            case "vkCreateSwapchainKHR":
                VkTrampolines.RealCreateSwapchainKHR =
                    (delegate* unmanaged[Cdecl]<Device, SwapchainCreateInfoKHR*, AllocationCallbacks*, SwapchainKHR*, Result>)realPtr;
                return MarkHooked(name, (IntPtr)(delegate* unmanaged[Cdecl]<Device, SwapchainCreateInfoKHR*, AllocationCallbacks*, SwapchainKHR*, Result>)&VkTrampolines.CreateSwapchainKHR);

            case "vkGetSwapchainImagesKHR":
                VkTrampolines.RealGetSwapchainImagesKHR =
                    (delegate* unmanaged[Cdecl]<Device, SwapchainKHR, uint*, Image*, Result>)realPtr;
                return MarkHooked(name, (IntPtr)(delegate* unmanaged[Cdecl]<Device, SwapchainKHR, uint*, Image*, Result>)&VkTrampolines.GetSwapchainImagesKHR);

            case "vkAcquireNextImageKHR":
                VkTrampolines.RealAcquireNextImageKHR =
                    (delegate* unmanaged[Cdecl]<Device, SwapchainKHR, ulong, Semaphore, Fence, uint*, Result>)realPtr;
                return MarkHooked(name, (IntPtr)(delegate* unmanaged[Cdecl]<Device, SwapchainKHR, ulong, Semaphore, Fence, uint*, Result>)&VkTrampolines.AcquireNextImageKHR);

            case "vkQueuePresentKHR":
                VkTrampolines.RealQueuePresentKHR =
                    (delegate* unmanaged[Cdecl]<Queue, PresentInfoKHR*, Result>)realPtr;
                return MarkHooked(name, (IntPtr)(delegate* unmanaged[Cdecl]<Queue, PresentInfoKHR*, Result>)&VkTrampolines.QueuePresentKHR);

            default:
                return realPtr;
        }
    }

    private static IntPtr MarkHooked(string name, IntPtr trampoline)
    {
        HookedFunctions.Add(name);
        TraceLog.Log($"[VkProcInterceptor] installed trampoline for {name}");
        return trampoline;
    }

    private static void EnsureRealGetPhysicalDeviceProperties2(IntPtr vkInstance)
    {
        if (VkTrampolines.RealGetPhysicalDeviceProperties2 != null) return;

        var p = LookupReal(vkInstance, "vkGetPhysicalDeviceProperties2");
        if (p == IntPtr.Zero)
            p = LookupReal(vkInstance, "vkGetPhysicalDeviceProperties2KHR");

        if (p != IntPtr.Zero)
        {
            VkTrampolines.RealGetPhysicalDeviceProperties2 =
                (delegate* unmanaged[Cdecl]<PhysicalDevice, PhysicalDeviceProperties2*, void>)p;
            TraceLog.Log("[VkProcInterceptor] pre-loaded vkGetPhysicalDeviceProperties2 for LUID matching");
        }
        else
        {
            TraceLog.Log("[VkProcInterceptor] WARNING: vkGetPhysicalDeviceProperties2 unavailable; LUID reorder disabled");
        }
    }

    private static IntPtr LookupReal(IntPtr vkInstance, string name)
    {
        var pName = Marshal.StringToHGlobalAnsi(name);
        try
        {
            return VulkanLoader.VkGetInstanceProcAddr(vkInstance, pName);
        }
        finally
        {
            Marshal.FreeHGlobal(pName);
        }
    }

    public static IReadOnlyCollection<string> ObservedHookableFunctions => HookedFunctions;
}
