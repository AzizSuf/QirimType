using System.Diagnostics;
using Microsoft.Win32;

namespace QirimType.Utils;

public static class AutostartManager
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "QirimType";

    public static bool IsAutostartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
            if (key == null) return false;

            var value = key.GetValue(AppName) as string;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check autostart: {ex.Message}");
            return false;
        }
    }

    public static bool SetAutostart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
            if (key == null) return false;

            if (enable)
            {
                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = Application.ExecutablePath;
                }
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set autostart: {ex.Message}");
            return false;
        }
    }
}
