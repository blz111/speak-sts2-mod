using System.Text;
using Godot;

namespace Sts2Speak.Diagnostics;

internal static class RuntimeTrace
{
    private static readonly object SyncRoot = new();

    public static void Write(string message)
    {
        try
        {
            string path = ProjectSettings.GlobalizePath("user://runtime_trace.log");
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{System.Environment.NewLine}";

            lock (SyncRoot)
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(path, line, Encoding.UTF8);
            }
        }
        catch
        {
        }
    }
}
