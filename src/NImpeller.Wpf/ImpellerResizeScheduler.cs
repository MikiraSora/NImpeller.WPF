using System;
using System.Windows.Threading;

namespace NImpeller.Wpf;

internal sealed class ImpellerResizeScheduler
{
    private readonly Dispatcher _dispatcher;
    private readonly Func<(uint Width, uint Height)> _getTargetSize;
    private readonly Func<(uint Width, uint Height)> _getCurrentSize;
    private readonly Action<uint, uint> _resize;
    private DispatcherTimer? _timer;
    private bool _resizeInProgress;

    public ImpellerResizeScheduler(
        Dispatcher dispatcher,
        Func<(uint Width, uint Height)> getTargetSize,
        Func<(uint Width, uint Height)> getCurrentSize,
        Action<uint, uint> resize)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _getTargetSize = getTargetSize ?? throw new ArgumentNullException(nameof(getTargetSize));
        _getCurrentSize = getCurrentSize ?? throw new ArgumentNullException(nameof(getCurrentSize));
        _resize = resize ?? throw new ArgumentNullException(nameof(resize));
    }

    public bool IsEnabled { get; set; }

    public void ScheduleIfChanged()
    {
        if (!IsEnabled) return;

        var target = _getTargetSize();
        var current = _getCurrentSize();
        if (target.Width == current.Width && target.Height == current.Height) return;

        EnsureTimer();
        _timer!.Stop();
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    private void EnsureTimer()
    {
        _timer ??= new DispatcherTimer(
            TimeSpan.FromMilliseconds(16),
            DispatcherPriority.Background,
            OnTick,
            _dispatcher);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer!.Stop();
        if (!IsEnabled) return;
        if (_resizeInProgress) return;

        var target = _getTargetSize();
        var current = _getCurrentSize();
        if (target.Width == current.Width && target.Height == current.Height) return;
        if (target.Width < 16 || target.Height < 16) return;

        _resizeInProgress = true;
        try
        {
            _resize(target.Width, target.Height);
        }
        finally
        {
            _resizeInProgress = false;
        }

        var nextTarget = _getTargetSize();
        var nextCurrent = _getCurrentSize();
        if ((nextTarget.Width != nextCurrent.Width || nextTarget.Height != nextCurrent.Height) &&
            nextTarget.Width >= 16 && nextTarget.Height >= 16)
        {
            _timer!.Stop();
            _timer.Start();
        }
    }
}
