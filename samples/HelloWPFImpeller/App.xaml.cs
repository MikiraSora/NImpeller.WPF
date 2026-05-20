using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace HelloWPFImpeller;

public partial class App : Application
{
    [DllImport("kernel32")]
    private static extern bool AllocConsole();

    public static string LogFilePath { get; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run.log");

    public static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Console.WriteLine(line);
        Debug.WriteLine(line);
        try { File.AppendAllText(LogFilePath, line + Environment.NewLine); }
        catch { /* swallow IO errors so logging never crashes the app */ }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
#if DEBUG
        AllocConsole();
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Trace.Listeners.Add(new ConsoleTraceListener());
#endif
        try { File.Delete(LogFilePath); } catch { }
        Log("--- HelloWPFImpeller started ---");
        base.OnStartup(e);
    }
}
