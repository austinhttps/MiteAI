using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace MiteAI.App;

public class StartupManager
{
    public const string AppName = "MiteAI";
    public const string RunRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _subKeyPath;
    private readonly string _appName;

    public StartupManager(string appName = AppName, string subKeyPath = RunRegistryKeyPath)
    {
        _appName = appName;
        _subKeyPath = subKeyPath;
    }

    public static string GetExecutablePath()
    {
        return Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName
            ?? string.Empty;
    }

    public bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_subKeyPath, writable: false);
        return key?.GetValue(_appName) != null;
    }

    public bool EnableStartup()
    {
        string exePath = GetExecutablePath();
        if (string.IsNullOrEmpty(exePath))
        {
            return false;
        }

        using var key = Registry.CurrentUser.OpenSubKey(_subKeyPath, writable: true);
        if (key == null)
        {
            return false;
        }

        key.SetValue(_appName, $"\"{exePath}\"");
        return true;
    }

    public bool DisableStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_subKeyPath, writable: true);
        if (key == null)
        {
            return false;
        }

        key.DeleteValue(_appName, throwOnMissingValue: false);
        return true;
    }

    public bool ToggleStartup()
    {
        if (IsStartupEnabled())
        {
            DisableStartup();
            return false;
        }

        EnableStartup();
        return true;
    }

    // Static helper methods
    public static bool IsEnabled() => new StartupManager().IsStartupEnabled();
    public static bool Enable() => new StartupManager().EnableStartup();
    public static bool Disable() => new StartupManager().DisableStartup();
    public static bool Toggle() => new StartupManager().ToggleStartup();
}
