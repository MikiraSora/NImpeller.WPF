using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace NImpeller.Wpf.Interop;

/// <summary>
/// Singleton dispatcher that subscribes to <see cref="CompositionTarget.Rendering"/>
/// exactly once and fans the tick out to every registered <see cref="ImpellerView"/>
/// with <c>RenderContinuously = true</c>.
///
/// All registration/unregistration and per-frame dispatch happens on the WPF UI thread,
/// so the registration list does not need an external lock.
/// </summary>
internal static class ViewTicker
{
    public delegate void TickCallback();

    private static readonly List<TickCallback> Subscribers = new();
    private static bool _subscribed;

    public static void Register(TickCallback callback)
    {
        if (!Subscribers.Contains(callback))
            Subscribers.Add(callback);
        EnsureSubscribed();
    }

    public static void Unregister(TickCallback callback)
    {
        Subscribers.Remove(callback);
        if (Subscribers.Count == 0 && _subscribed)
        {
            CompositionTarget.Rendering -= OnRendering;
            _subscribed = false;
        }
    }

    private static void EnsureSubscribed()
    {
        if (_subscribed) return;
        CompositionTarget.Rendering += OnRendering;
        _subscribed = true;
    }

    private static void OnRendering(object? sender, EventArgs e)
    {
        for (int i = 0; i < Subscribers.Count; i++)
        {
            try { Subscribers[i](); }
            catch (Exception ex)
            {
                TraceLog.Log($"[ViewTicker] subscriber threw: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
