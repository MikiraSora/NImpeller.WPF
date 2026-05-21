using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
// ReSharper disable InconsistentNaming

namespace NImpeller;

/// <summary>Managed owner for an Impeller rendering context.</summary>
public unsafe partial class ImpellerContext
{
    private sealed class TextureUploadBuffer
    {
        private nint _data;

        public TextureUploadBuffer(ReadOnlySpan<byte> contents)
        {
            Length = contents.Length;
            _data = (nint)NativeMemory.Alloc((nuint)Length);
            contents.CopyTo(new Span<byte>((void*)_data, Length));
        }

        public byte* Data => (byte*)_data;
        public int Length { get; }

        public void Release()
        {
            var data = Interlocked.Exchange(ref _data, 0);
            if (data != 0)
                NativeMemory.Free((void*)data);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static IntPtr GetProcAddressCallback(IntPtr proc, IntPtr userData)
    {
        return ((Func<IntPtr, IntPtr>)GCHandle.FromIntPtr(userData).Target!)(proc!);
    }
    
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static IntPtr GetVulkanProcAddressCallback(IntPtr vkInstance, IntPtr proc, IntPtr userData)
    {
        return ((Func<IntPtr, IntPtr, IntPtr>)GCHandle.FromIntPtr(userData).Target!)(vkInstance, proc!);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static void ReleaseTextureUploadBufferCallback(IntPtr userData)
    {
        ReleaseTextureUploadBuffer(userData);
    }

    private static void ReleaseTextureUploadBuffer(IntPtr userData)
    {
        if (userData == IntPtr.Zero) return;

        var handle = GCHandle.FromIntPtr(userData);
        if (handle.Target is TextureUploadBuffer buffer)
            buffer.Release();
        handle.Free();
    }

    /// <summary>Create a Vulkan-backed Impeller context using a name-based proc-address resolver.</summary>
    /// <param name="getProcAddress">Callback that resolves Vulkan procedure names for the supplied instance.</param>
    /// <param name="enableValidation">Whether Vulkan validation should be requested for the context.</param>
    /// <returns>A new context, or null if native context creation failed.</returns>
    public static ImpellerContext? CreateVulkanNew(Func<IntPtr, string, IntPtr> getProcAddress, bool enableValidation)
        => CreateVulkanNew((IntPtr vkInstance, IntPtr proc) =>
            getProcAddress(vkInstance, Marshal.PtrToStringAnsi(proc)!), enableValidation);
    
    /// <summary>Create a Vulkan-backed Impeller context using a raw proc-address resolver.</summary>
    /// <param name="getProcAddress">Callback that resolves Vulkan procedure names from native string pointers.</param>
    /// <param name="enableValidation">Whether Vulkan validation should be requested for the context.</param>
    /// <returns>A new context, or null if native context creation failed.</returns>
    public static ImpellerContext? CreateVulkanNew(Func<IntPtr, IntPtr, IntPtr> getProcAddress, bool enableValidation)
    {
        var handle = GCHandle.Alloc(getProcAddress);
        var settings = new ImpellerContextVulkanSettings
        {
            User_data = GCHandle.ToIntPtr(handle),
            Enable_vulkan_validation = enableValidation ? 1 : 0,
            Proc_address_callback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr>)&GetVulkanProcAddressCallback,
        };
        var res = UnsafeNativeMethods.ImpellerContextCreateVulkanNew(UnsafeNativeMethods.ImpellerVersion, &settings);
        handle.Free();
        return res != null! ? new ImpellerContext(res) : null;
    }

    /// <summary>Create an OpenGL ES-backed Impeller context using a name-based proc-address resolver.</summary>
    /// <param name="getProcAddress">Callback that resolves OpenGL ES procedure names.</param>
    /// <returns>A new context, or null if native context creation failed.</returns>
    public static ImpellerContext? CreateOpenGLESNew(Func<string, IntPtr> getProcAddress)
        => CreateOpenGLESNew((IntPtr name) => getProcAddress(Marshal.PtrToStringAnsi(name)!));
    
    /// <summary>Create an OpenGL ES-backed Impeller context using a raw proc-address resolver.</summary>
    /// <param name="getProcAddress">Callback that resolves OpenGL ES procedure names from native string pointers.</param>
    /// <returns>A new context, or null if native context creation failed.</returns>
    public static ImpellerContext? CreateOpenGLESNew(Func<IntPtr, IntPtr> getProcAddress)
    {
        var handle = GCHandle.Alloc(getProcAddress);
        var res = UnsafeNativeMethods.ImpellerContextCreateOpenGLESNew(UnsafeNativeMethods.ImpellerVersion,
            (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr>)&GetProcAddressCallback,
            GCHandle.ToIntPtr(handle));
        handle.Free();
        return res != null! ? new ImpellerContext(res) : null;
    }

    /// <summary>Create a Metal-backed Impeller context on platforms where Metal is available.</summary>
    /// <returns>A new context, or null if native context creation failed.</returns>
    public static ImpellerContext? CreateMetalNew()
    {
        var res = UnsafeNativeMethods.ImpellerContextCreateMetalNew(UnsafeNativeMethods.ImpellerVersion);
        return res != null! ? new ImpellerContext(res) : null;
    }

    /// <summary>Get Vulkan handles and queue information for a Vulkan-backed context.</summary>
    /// <returns>Vulkan context information, or null if the current context is not Vulkan-backed.</returns>
    public ImpellerContextVulkanInfo? GetVulkanInfo()
    {
        ImpellerContextVulkanInfo info;
        if (UnsafeNativeMethods.ImpellerContextGetVulkanInfo(Handle, &info) == 0)
            return null;
        return info;
    }

    /// <summary>Create a texture from tightly packed RGBA8888 pixel data.</summary>
    /// <param name="descriptor">Texture descriptor that matches the supplied pixel data.</param>
    /// <param name="contents">Tightly packed decompressed texture contents.</param>
    /// <returns>A new texture, or null if native texture creation failed.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="contents"/> is empty.</exception>
    public ImpellerTexture? TextureCreateWithContentsNew(
        ImpellerTextureDescriptor descriptor,
        ReadOnlySpan<byte> contents)
    {
        if (contents.IsEmpty)
            throw new ArgumentException("Texture contents must not be empty.", nameof(contents));

        var buffer = new TextureUploadBuffer(contents);
        var userData = GCHandle.ToIntPtr(GCHandle.Alloc(buffer));
        var mapping = new ImpellerMapping
        {
            Data = buffer.Data,
            Length = (ulong)buffer.Length,
            On_release = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, void>)&ReleaseTextureUploadBufferCallback,
        };

        try
        {
            var ret = UnsafeNativeMethods.ImpellerTextureCreateWithContentsNew(Handle, &descriptor, &mapping, userData);
            if (ret != null)
                return new ImpellerTexture(ret);
        }
        catch
        {
            ReleaseTextureUploadBuffer(userData);
            throw;
        }

        return null;
    }
}
