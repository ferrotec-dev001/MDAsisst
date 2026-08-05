using Microsoft.Win32;
using MDAsisst.Core.Logging;

namespace MDAsisst.App.Services;

/// <summary>Windows ログオン時の自動起動登録（FR-WN-08）。HKCU のみ操作し管理者権限を要求しない。</summary>
public static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MDAsisst";

    public static bool IsEnabled(ILogSink? log = null)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            return key?.GetValue(ValueName) is not null;
        }
        catch (Exception ex)
        {
            (log ?? NullLogSink.Instance).Warn("自動起動設定の読み取りに失敗しました。", ex);
            return false;
        }
    }

    public static bool SetEnabled(bool enabled, ILogSink? log = null)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
            if (key is null) return false;

            if (enabled)
            {
                var exe = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exe)) return false;
                key.SetValue(ValueName, $"\"{exe}\" --tray");
            }
            else if (key.GetValue(ValueName) is not null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
            return true;
        }
        catch (Exception ex)
        {
            (log ?? NullLogSink.Instance).Warn("自動起動設定の変更に失敗しました。", ex);
            return false;
        }
    }
}
