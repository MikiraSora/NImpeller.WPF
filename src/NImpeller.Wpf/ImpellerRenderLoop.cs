using System;
using System.Diagnostics;
using System.Windows.Threading;

using NImpeller.Wpf.Interop;

namespace NImpeller.Wpf;

internal readonly struct ImpellerFrameTiming
{
    internal ImpellerFrameTiming(TimeSpan deltaTime, TimeSpan totalTime, long frameNumber)
    {
        DeltaTime = deltaTime;
        TotalTime = totalTime;
        FrameNumber = frameNumber;
    }

    internal TimeSpan DeltaTime { get; }
    internal TimeSpan TotalTime { get; }
    internal long FrameNumber { get; }
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

    internal ImpellerRenderLoop(Dispatcher dispatcher, Action renderFrame)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _renderFrame = renderFrame ?? throw new ArgumentNullException(nameof(renderFrame));
    }

    internal bool StartRequested { get; private set; }
    internal bool IsRunning => _tickCallback != null;
    internal long FrameNumber => _frameNumber;

    internal void SetStartRequested(bool value)
    {
        StartRequested = value;
    }

    internal void RequestStart()
    {
        StartRequested = true;
    }

    internal void Start()
    {
        StartRequested = true;
        RegisterToTicker();
    }

    internal void Stop()
    {
        StartRequested = false;
        UnregisterFromTicker();
    }

    internal void Suspend()
    {
        _invalidateRequested = false;
        UnregisterFromTicker();
    }

    internal void InvalidateRender()
    {
        if (_invalidateRequested) return;

        _invalidateRequested = true;
        _dispatcher.BeginInvoke((Action)DispatchFrame);
    }

    internal void RestartClock()
    {
        _stopwatch.Restart();
    }

    internal ImpellerFrameTiming AdvanceFrame()
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
