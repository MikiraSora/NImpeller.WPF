using System;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.Direct3D9;
using Silk.NET.DXGI;

namespace NImpeller.Wpf.Interop;

/// <summary>
/// Manages a D3D9Ex device, a D3D11 device, and a shared render-target texture
/// that bridges Vulkan (Impeller's swapchain image, blitted into it) and
/// WPF's D3DImage (which receives the D3D9 surface view of the same memory).
///
/// Pipeline:
///   D3D9Ex CreateTexture(... shared handle d3d9shared)
///       -> backbufferTexture (IDirect3DTexture9)
///       -> backbufferSurface (IDirect3DSurface9)  ---> D3DImage.SetBackBuffer
///   d3d11device.OpenSharedResource&lt;ID3D11Texture2D&gt;(d3d9shared)
///       -> renderTargetTexture (ID3D11Texture2D)
///       -> renderTargetTexture.QueryInterface&lt;IDXGIResource&gt;().GetSharedHandle(out vulkanShared)
///           ---> consumed by Vulkan via VK_KHR_external_memory_win32 (D3D11TextureKmtBit)
///
/// One <c>D3DResources</c> instance per <c>ImpellerView</c> instance: D3D9 shared
/// handles are per-device, so sharing the D3D devices across views would tangle
/// each view's textures with the others'.
/// </summary>
internal sealed unsafe class D3DResources : IDisposable
{
    private readonly D3D11 _d3d11 = D3D11.GetApi(null);
    private readonly D3D9 _d3d9 = D3D9.GetApi(null);

    private ComPtr<ID3D11Device> _d3d11Device;
    private ComPtr<ID3D11DeviceContext> _d3d11Context;

    private ComPtr<IDirect3D9Ex> _d3d9Context;
    private ComPtr<IDirect3DDevice9Ex> _d3d9Device;

    private ComPtr<IDirect3DTexture9> _backbufferTexture;
    private ComPtr<IDirect3DSurface9> _backbufferSurface;
    private ComPtr<ID3D11Texture2D> _renderTargetTexture;

    private nint _vulkanSharedHandle;
    private Luid _adapterLuid;
    private uint _width;
    private uint _height;

    public ID3D11Device* D3D11Device => _d3d11Device;
    public ID3D11DeviceContext* D3D11Context => _d3d11Context;
    public ID3D11Texture2D* RenderTargetTexture => _renderTargetTexture;
    public IDirect3DSurface9* BackbufferSurface => _backbufferSurface;

    /// <summary>Native pointer to pass to D3DImage.SetBackBuffer(IDirect3DSurface9, ...).</summary>
    public nint BackbufferSurfaceHandle => (nint)_backbufferSurface.Handle;

    /// <summary>KMT shared handle to import on Vulkan side via VK_KHR_external_memory_win32.</summary>
    public nint VulkanSharedHandle => _vulkanSharedHandle;

    public Luid AdapterLuid => _adapterLuid;
    public uint Width => _width;
    public uint Height => _height;

    public void Initialize(nint windowHandle)
    {
        CreateD3D11Device();
        CreateD3D9ExDevice(windowHandle);
    }

    private void CreateD3D11Device()
    {
        SilkMarshal.ThrowHResult(_d3d11.CreateDevice(
            pAdapter: default(ComPtr<IDXGIAdapter>),
            DriverType: D3DDriverType.Hardware,
            Software: nint.Zero,
            Flags: (uint)CreateDeviceFlag.BgraSupport,
            pFeatureLevels: null,
            FeatureLevels: 0u,
            SDKVersion: D3D11.SdkVersion,
            ppDevice: ref _d3d11Device,
            pFeatureLevel: null,
            ppImmediateContext: ref _d3d11Context));
    }

    private void CreateD3D9ExDevice(nint windowHandle)
    {
        SilkMarshal.ThrowHResult(_d3d9.Direct3DCreate9Ex(D3D9.SdkVersion, ref _d3d9Context));

        var presentParameters = new Silk.NET.Direct3D9.PresentParameters
        {
            Windowed = 1,
            SwapEffect = Swapeffect.Discard,
            PresentationInterval = unchecked((uint)D3D9.PresentIntervalImmediate),
        };

        const uint adapter = 0u;
        SilkMarshal.ThrowHResult(_d3d9Context.GetAdapterLUID(adapter, ref _adapterLuid));
        SilkMarshal.ThrowHResult(_d3d9Context.CreateDeviceEx(
            adapter,
            Devtype.Hal,
            windowHandle,
            D3D9.CreateHardwareVertexprocessing,
            ref presentParameters,
            null,
            ref _d3d9Device));
    }

    /// <summary>
    /// Create (or recreate after resize) the shared render-target chain.
    /// Disposes any previously created resources first.
    /// </summary>
    public void CreateOrResizeRenderTarget(uint width, uint height)
    {
        DisposeRenderTargetResources();

        _width = width;
        _height = height;

        void* d3d9SharedHandle = null;
        SilkMarshal.ThrowHResult(_d3d9Device.CreateTexture(
            Width: width,
            Height: height,
            Levels: 1u,
            Usage: D3D9.UsageRendertarget,
            Format: Silk.NET.Direct3D9.Format.X8R8G8B8,
            Pool: Pool.Default,
            ppTexture: ref _backbufferTexture,
            pSharedHandle: ref d3d9SharedHandle));

        SilkMarshal.ThrowHResult(_backbufferTexture.GetSurfaceLevel(0u, ref _backbufferSurface));

        _renderTargetTexture = _d3d11Device.OpenSharedResource<ID3D11Texture2D>(d3d9SharedHandle);

        void* vulkanHandle;
        var resource = _renderTargetTexture.QueryInterface<IDXGIResource>();
        try
        {
            SilkMarshal.ThrowHResult(resource.GetSharedHandle(&vulkanHandle));
        }
        finally
        {
            resource.Dispose();
        }
        _vulkanSharedHandle = (nint)vulkanHandle;
    }

    private void DisposeRenderTargetResources()
    {
        if (_renderTargetTexture.Handle != null) { _renderTargetTexture.Dispose(); _renderTargetTexture = default; }
        if (_backbufferSurface.Handle != null) { _backbufferSurface.Dispose(); _backbufferSurface = default; }
        if (_backbufferTexture.Handle != null) { _backbufferTexture.Dispose(); _backbufferTexture = default; }
        _vulkanSharedHandle = 0;
    }

    public void Dispose()
    {
        DisposeRenderTargetResources();
        if (_d3d9Device.Handle != null) { _d3d9Device.Dispose(); _d3d9Device = default; }
        if (_d3d9Context.Handle != null) { _d3d9Context.Dispose(); _d3d9Context = default; }
        if (_d3d11Context.Handle != null) { _d3d11Context.Dispose(); _d3d11Context = default; }
        if (_d3d11Device.Handle != null) { _d3d11Device.Dispose(); _d3d11Device = default; }
        _d3d11.Dispose();
        _d3d9.Dispose();
    }
}
