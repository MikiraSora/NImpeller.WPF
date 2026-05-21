using System;
using System.Diagnostics;
using System.Windows.Threading;

using NImpeller.Wpf.Interop;

namespace NImpeller.Wpf;

internal readonly struct ImpellerFrameTiming
{
    public ImpellerFrameTiming(TimeSpan deltaTime, TimeSpan totalTime, long frameNumber)
    {
        DeltaTime = deltaTime;
        TotalTime = totalTime;
        FrameNumber = frameNumber;
    }

    public TimeSpan DeltaTime { get; }
    public TimeSpan TotalTime { get; }
    public long FrameNumber { get; }
}

internal sealed class ImpellerRenderLoop
{
    private readonly Dispatcher _dispatcher;
    private readonly Action _renderFrame;
    private readonly Stopwatch _stopwatch = new();
    private ViewTicker.TickCallback? _tickCallback;
    private TimeSpan _lastFrameTime = TimeSpan.Zero;
    private long _frameNumber;
    private bool _invalidateRequested;

    public ImpellerRenderLoop(Dispatcher dispatcher, Action renderFrame)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _renderFrame = renderFrame ?? throw new ArgumentNullException(nameof(renderFrame));
    }

    public bool StartRequested { get; private set; }
    public bool IsRunning => _tickCallback != null;
    public long FrameNumber => _frameNumber;

    public void SetStartRequested(bool value)
    {
        StartRequested = value;
    }

    public void RequestStart()
    {
        StartRequested = true;
    }

    public void Start()
    {
        StartRequested = true;
        RegisterToTicker();
    }

    public void Stop()
    {
        StartRequested = false;
        UnregisterFromTicker();
    }

    public void Suspend()
    {
        _invalidateRequested = false;
        UnregisterFromTicker();
    }

    public void InvalidateRender()
    {
        if (_invalidateRequested) return;

        _invalidateRequested = true;
        _dispatcher.BeginInvoke((Action)DispatchFrame);
    }

    public void RestartClock()
    {
        _stopwatch.Restart();
    }

    public ImpellerFrameTiming AdvanceFrame()
    {
        var totalTime = _stopwatch.Elapsed;
        var deltaTime = totalTime - _lastFrameTime;
        _lastFrameTime = totalTime;
        _frameNumber++;

        return new ImpellerFrameTiming(deltaTime, totalTime, _frameNumber);
    }

    private void RegisterToTicker()
    {
        if (_tickCallback != null) return;

        _tickCallback = DispatchFrame;
        ViewTicker.Register(_tickCallback);
    }

    private void UnregisterFromTicker()
    {
        if (_tickCallback == null) return;

        ViewTicker.Unregister(_tickCallback);
        _tickCallback = null;
    }

    private void DispatchFrame()
    {
        _invalidateRequested = false;
        _renderFrame();
    }
}
