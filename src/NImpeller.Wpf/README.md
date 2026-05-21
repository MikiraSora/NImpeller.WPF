# NImpeller.Wpf

A WPF control library that brings the [Impeller](https://github.com/flutter/engine/tree/main/impeller) 2D rendering
engine (the Vulkan backend powering Flutter) to WPF applications. The `ImpellerView` control hands you an
`ImpellerDisplayListBuilder` once per frame and renders the result into your WPF visual tree via `D3DImage`.

The control's public surface is intentionally small: declare a control in XAML, wire up a
`Render` event, call `Start()` from code-behind, and draw.

```xml
<imp:ImpellerView x:Name="View" Render="View_OnRender"/>
```

```csharp
View.Start();
// ...
private void View_OnRender(object? s, ImpellerRenderEventArgs e)
{
    using var paint = ImpellerPaint.New()!;
    paint.SetColor(ImpellerColor.FromRgb(0xE8, 0x6F, 0x6F));
    e.Builder.DrawRect(new ImpellerRect(40, 40, 200, 140), paint);
}
```

---

## Overview

NImpeller.Wpf is a thin glue layer between three pieces:

| Layer | Role |
| --- | --- |
| [Impeller](https://github.com/flutter/engine/tree/main/impeller) (native `impeller.dll`) | The actual 2D renderer — paths, gradients, blends, typography, etc. |
| [NImpeller](../NImpeller/) | Hand-authored + generated .NET P/Invoke bindings over `impeller.h`. |
| **NImpeller.Wpf** (this library) | A WPF `FrameworkElement` (`ImpellerView`) that wires Impeller's Vulkan backend to WPF's `D3DImage`, with full multi-instance support. |

Internally the library:

1. Boots **one** `ImpellerContext` per process (Impeller's Vulkan instance/device are expensive — ~1–2 s — and
   shared by every view).
2. Creates a per-view D3D9Ex + D3D11 shared texture; imports its KMT handle as a `VkImage` on Impeller's
   `VkDevice` via `VK_KHR_external_memory_win32`.
3. Intercepts a handful of Vulkan entry points the first time Impeller resolves them, so that on every
   `vkQueuePresentKHR` the freshly rendered swapchain image is `vkCmdBlitImage`-blitted into the per-view
   shared `VkImage` instead of being presented to a window.
4. Exposes the shared texture's `IDirect3DSurface9` to WPF through `D3DImage`. The view's `OnRender`
   overrides `dc.DrawImage(_d3dImage, …)` so it participates in the normal WPF visual tree.

The net effect: a `<imp:ImpellerView/>` looks and behaves like any other WPF element, but every pixel inside
it is drawn by Impeller's Vulkan renderer.

---

## Features

- **`ImpellerView : FrameworkElement`** — composes naturally with WPF layout. Use it in `Grid`, `DockPanel`,
  `TabControl`, anywhere a `FrameworkElement` works.
- **Multi-instance support** — host as many `ImpellerView` controls in a window as you like. Each gets its
  own swapchain + shared texture; they share the underlying `ImpellerContext`.
- **DPI awareness** — render target is allocated in physical pixels; the WPF visual is mapped back to DIPs by
  the matching DPI on the `D3DImage`, so text and 1‑px strokes stay sharp on 125 %/150 %/200 % displays.
- **Minimal API** — `Render` event + `Start()` / `Stop()` / `InvalidateRender()`, with
  `InitializeRender(settings)` available when custom settings are needed. No dependency
  properties, no MVVM ceremony.
- **Continuous or on-demand rendering** — `RenderContinuously = true` (default) drives every WPF frame via a
  single shared `CompositionTarget.Rendering` subscription; `false` only redraws when you call
  `InvalidateRender()`.
- **Automatic lifecycle** — initialization on `Loaded`, teardown on `Unloaded`, GPU resource cleanup on
  `AppDomain.ProcessExit`. Reopening windows reuses the existing Impeller context.
- **Resize-aware** — swapchain and shared texture are rebuilt on `SizeChanged` (coalesced via the dispatcher).

---

## Installation

NImpeller.Wpf is not yet published to NuGet. Reference it as a project from this repository:

```xml
<!-- YourApp.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <!-- Required SDK-style settings -->
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <Platforms>x64</Platforms>
  </PropertyGroup>

  <ItemGroup>
    <!-- The .NET bindings + the WPF control -->
    <ProjectReference Include="..\..\src\NImpeller\NImpeller.csproj" />
    <ProjectReference Include="..\..\src\NImpeller.Wpf\NImpeller.Wpf.csproj" />
  </ItemGroup>

</Project>
```

And then deploys impeller.dll and others to your app's exe

Three things to note:

- **`UseWPF=true`** is mandatory — `ImpellerView` is a `FrameworkElement`.
- **`AllowUnsafeBlocks=true`** is mandatory — the library uses `unsafe` pointer code for Vulkan interop.
- **`Platforms=x64`** is mandatory.
- `Impeller.targets` copies `impeller.dll` from `external/impeller_sdk/{platform}/lib/` to your app's output
  directory. NImpeller.Wpf itself does **not** import this file so library consumers don't accidentally bring
  in a second copy; your application is the right place to deploy native binaries.

---

## Usage

### 1. Declare an `ImpellerView` in XAML

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:imp="clr-namespace:NImpeller.Wpf;assembly=NImpeller.Wpf"
        Width="800" Height="600">
    <Grid>
        <imp:ImpellerView x:Name="View" Render="View_OnRender"/>
    </Grid>
</Window>
```

### 2. Start the view from code-behind

```csharp
using NImpeller;
using NImpeller.Wpf;
using System.Windows;

namespace MyApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Default settings — RenderContinuously = true, UseDeviceDpi = true.
        View.Start();

        // Or initialize with custom settings:
        // View.InitializeRender(new ImpellerViewSettings {
        //     RenderContinuously = false,    // only render on InvalidateRender()
        //     UseDeviceDpi       = true,     // physical-pixel render target
        //     EnableValidation   = false,    // honored only by the first view in the process
        //     LogicalSizeOverride = null,    // null = fill parent, otherwise a fixed Size in DIPs
        // });
    }

    private void View_OnRender(object? sender, ImpellerRenderEventArgs e)
    {
        // e.Builder       — fresh ImpellerDisplayListBuilder, will be drawn after this handler returns
        // e.Typography    — shared ImpellerTypographyContext (nullable)
        // e.PixelWidth/Height — backing render-target size in pixels
        // e.DpiScaleX/Y   — this view's DPI scale (multiply font sizes / strokes by this for crispness)
        // e.DeltaTime     — time since this view's previous frame
        // e.TotalTime     — elapsed render-loop time for this view
        // e.FrameNumber   — monotonically increasing frame counter

        // Background fill
        using var bg = ImpellerPaint.New()!;
        bg.SetColor(ImpellerColor.FromRgb(0x1A, 0x1D, 0x22));
        e.Builder.DrawPaint(bg);

        // A red rounded rect
        using var paint = ImpellerPaint.New()!;
        paint.SetColor(ImpellerColor.FromRgb(0xE8, 0x6F, 0x6F));
        var corner = new ImpellerPoint { X = 16, Y = 16 };
        var radii = new ImpellerRoundingRadii {
            Top_left = corner, Top_right = corner,
            Bottom_left = corner, Bottom_right = corner,
        };
        e.Builder.DrawRoundedRect(new ImpellerRect(40, 40, 200, 140), radii, paint);
    }
}
```

### 3. Multiple `ImpellerView`s in the same window

Each view is fully independent — different sizes, different scenes, different `RenderContinuously` modes:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition/><RowDefinition/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition/><ColumnDefinition/>
    </Grid.ColumnDefinitions>
    <imp:ImpellerView Grid.Row="0" Grid.Column="0" x:Name="V1" Render="V1_OnRender"/>
    <imp:ImpellerView Grid.Row="0" Grid.Column="1" x:Name="V2" Render="V2_OnRender"/>
    <imp:ImpellerView Grid.Row="1" Grid.Column="0" x:Name="V3" Render="V3_OnRender"/>
    <imp:ImpellerView Grid.Row="1" Grid.Column="1" x:Name="V4" Render="V4_OnRender"/>
</Grid>
```

```csharp
public MainWindow()
{
    InitializeComponent();
    V1.Start(); V2.Start(); V3.Start(); V4.Start();
}
```

The first `Start()` or `InitializeRender()` lazily creates a process-wide `ImpellerSharedHost`; subsequent views reuse it.

### 4. On-demand rendering

For UI that doesn't need 60 fps:

```csharp
View.InitializeRender(new ImpellerViewSettings { RenderContinuously = false });

// ...later, when your data changes:
View.InvalidateRender();
```

### 5. API surface

```csharp
public sealed class ImpellerView : FrameworkElement
{
    public event EventHandler<ImpellerRenderEventArgs>? Render;
    public event EventHandler? Ready; // fires once after the first successful frame

    public void InitializeRender();
    public void InitializeRender(ImpellerViewSettings settings);
    public void Start();
    public void InvalidateRender();
    public void Stop();

    public bool   IsStarted   { get; }
    public int    PixelWidth  { get; }
    public int    PixelHeight { get; }
    public double DpiScaleX   { get; }
    public double DpiScaleY   { get; }
    public long   FrameNumber { get; }
}

public sealed class ImpellerViewSettings
{
    public bool  RenderContinuously   { get; init; } = true;
    public bool  UseDeviceDpi         { get; init; } = true;
    public bool  EnableValidation     { get; init; } = false; // first view only
    public Size? LogicalSizeOverride  { get; init; }
}

public sealed class ImpellerRenderEventArgs : EventArgs
{
    public ImpellerView                 Source       { get; }
    public ImpellerDisplayListBuilder   Builder      { get; }
    public ImpellerTypographyContext?   Typography   { get; }
    public int                          PixelWidth   { get; }
    public int                          PixelHeight  { get; }
    public float                        DpiScaleX    { get; }
    public float                        DpiScaleY    { get; }
    public TimeSpan                     DeltaTime    { get; }
    public TimeSpan                     TotalTime    { get; }
    public long                         FrameNumber  { get; }
}
```

---

## Building

From the repository root:

```bash
# Build just the library:
dotnet build src/NImpeller.Wpf/NImpeller.Wpf.csproj -c Debug

# Build everything (library + sample apps):
dotnet build NImpeller.sln -c Debug
```

Run the included samples to see it in action:

```bash
# Four ImpellerView instances in a 2x2 grid, each rendering an animated scene
dotnet run --project samples/HelloWPFImpeller -c Debug

# Gallery of Impeller capabilities — pick a scene from the left, see it render on the right
dotnet run --project samples/HelloWPFImpellerGallery -c Debug
```

---

## Diagnostics

The library writes diagnostic output via `System.Diagnostics.Trace` under the category `NImpeller.Wpf`. To
capture it, attach a listener in your app startup:

```csharp
Trace.Listeners.Add(new ConsoleTraceListener());
// or:
Trace.Listeners.Add(new TextWriterTraceListener("nimpeller-wpf.log"));
```

You'll see traces from `[ImpellerSharedHost]`, `[ImpellerView]`, `[VkProcInterceptor]`, `[VkTrampolines]`,
`[SharedVulkanImage]`, and `[HiddenVulkanWindow]` — useful when diagnosing GPU adapter selection, DPI
detection, swapchain creation, or blit failures.

---

## How it works (short version)

1. The first view initialization requested by `ImpellerView.Start()` or `ImpellerView.InitializeRender(settings)` boots an `ImpellerSharedHost` that loads `vulkan-1.dll`, creates the
   `ImpellerContext` (which internally creates a `VkInstance` + `VkDevice`), and caches everything.
2. Before Impeller resolves any Vulkan function, our `VkProcInterceptor` is installed as the
   `vkGetInstanceProcAddr` callback Impeller asks for. For nine entry points
   (`vkCreateInstance`, `vkCreateDevice`, `vkEnumeratePhysicalDevices`,
   `vkGetPhysicalDeviceSurfaceCapabilitiesKHR`, `vkCreateSwapchainKHR`, `vkGetSwapchainImagesKHR`,
   `vkAcquireNextImageKHR`, `vkQueuePresentKHR`, `vkGetPhysicalDeviceProperties2`) we hand back a trampoline
   that augments the call.
3. Each `ImpellerView` creates a per-view `D3DResources` (D3D9Ex shared texture + D3D11 imported handle),
   imports the texture as a `VkImage` on Impeller's device via `VK_KHR_external_memory_win32`, creates a
   hidden HWND sized to the view's physical render target, derives a `VkSurfaceKHR` from it, and asks Impeller for a swapchain.
4. The `vkCreateSwapchainKHR` trampoline appends `TRANSFER_SRC_BIT` to the image usage and binds a
   per-view `BlitContext` (command pool + buffer + fence + target image) to the resulting `VkSwapchainKHR`.
5. Every frame: `ImpellerSurface.Present()` triggers `vkQueuePresentKHR`. Our trampoline looks up the
   `BlitContext` by swapchain handle, records and submits a `vkCmdBlitImage` from the swapchain image to the
   D3D-shared `VkImage`, uses a per-view fence to serialize command-buffer reuse, and then calls the real
   present (with the wait semaphores stripped) so the swapchain advances.
6. WPF picks up the freshly written shared texture through `D3DImage.AddDirtyRect` and the view's
   `OnRender` blits it via `DrawingContext.DrawImage`.

---

## Screenshoots

<img width="1266" height="713" alt="image" src="https://github.com/user-attachments/assets/b6c4a81e-76b8-45dc-892c-75e26be7edea" />

<img width="1280" height="800" alt="image" src="https://github.com/user-attachments/assets/100f4af7-3e20-46a1-a7c3-400a837f6a43" />

---

## License

Same as the parent NImpeller repository.
