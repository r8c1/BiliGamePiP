using System;
using System.IO;
using System.Text.Json;
using BiliGamePiP.Models;

namespace BiliGamePiP.Services;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BiliGamePiP");

    public static string WebView2UserDataDirectory =>
        Path.Combine(AppDataDirectory, "WebView2Data");

    public static string SettingsFilePath =>
        Path.Combine(AppDataDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            Directory.CreateDirectory(AppDataDirectory);
            if (!File.Exists(SettingsFilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsFilePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (loaded is null)
            {
                return new AppSettings();
            }

            loaded.SeekSeconds = Math.Clamp(loaded.SeekSeconds, 1, 60);
            loaded.MiniOpacity = Math.Clamp(loaded.MiniOpacity, 0.35, 1.0);
            loaded.MiniWidth = Math.Clamp(loaded.MiniWidth, 260, 1920);
            loaded.MiniHeight = Math.Clamp(loaded.MiniHeight, 150, 1080);
            loaded.SeekBackward ??= new HotkeyBinding(0, System.Windows.Forms.Keys.NumPad7);
            loaded.PlayPause ??= new HotkeyBinding(0, System.Windows.Forms.Keys.NumPad8);
            loaded.SeekForward ??= new HotkeyBinding(0, System.Windows.Forms.Keys.NumPad9);
            loaded.ToggleClickThrough ??= new HotkeyBinding(0, System.Windows.Forms.Keys.NumPad0);
            loaded.ToggleMiniMode ??= new HotkeyBinding(0, System.Windows.Forms.Keys.Decimal);

            if (string.IsNullOrWhiteSpace(loaded.LastUrl))
            {
                loaded.LastUrl = "https://www.bilibili.com";
            }

            return loaded;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(AppDataDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // Ignore IO errors on shutdown
        }
    }
}
