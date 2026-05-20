using System.Diagnostics;

namespace NImpeller.Wpf.Interop;

/// <summary>
/// Internal diagnostic logging for the NImpeller.Wpf library. Writes to
/// <see cref="Trace"/> so consumers can attach their own <see cref="TraceListener"/>
/// (e.g. ConsoleTraceListener or TextWriterTraceListener) to capture output.
/// </summary>
internal static class TraceLog
{
    public const string Category = "NImpeller.Wpf";

    public static void Log(string message) => Trace.WriteLine(message, Category);
}
