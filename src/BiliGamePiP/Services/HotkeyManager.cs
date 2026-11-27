using System;
using System.Collections.Generic;
using BiliGamePiP.Models;
using BiliGamePiP.Native;

namespace BiliGamePiP.Services;

public enum HotkeyAction
{
    SeekBackward = 1001,
    PlayPause = 1002,
    SeekForward = 1003,
    ToggleClickThrough = 1004,
    ToggleMiniMode = 1005
}

public sealed class HotkeyManager : IDisposable
{
    private readonly IntPtr _windowHandle;
    private readonly HashSet<int> _registeredIds = new();

    public HotkeyManager(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public IReadOnlyList<string> Apply(AppSettings settings)
    {
        UnregisterAll();
        var failures = new List<string>();

        // 快进/后退允许按住连发，暂停/锁定/小窗切换禁止连发防抖动
        RegisterOne(HotkeyAction.SeekBackward, "后退", settings.SeekBackward, noRepeat: false, failures);
        RegisterOne(HotkeyAction.PlayPause, "暂停/播放", settings.PlayPause, noRepeat: true, failures);
        RegisterOne(HotkeyAction.SeekForward, "快进", settings.SeekForward, noRepeat: false, failures);
        RegisterOne(HotkeyAction.ToggleClickThrough, "鼠标穿透锁定", settings.ToggleClickThrough, noRepeat: true, failures);
        RegisterOne(HotkeyAction.ToggleMiniMode, "切换小窗置顶", settings.ToggleMiniMode, noRepeat: true, failures);

        return failures;
    }

    private void RegisterOne(
        HotkeyAction action,
        string label,
        HotkeyBinding binding,
        bool noRepeat,
        List<string> failures)
    {
        if (!binding.IsValid)
        {
            return;
        }

        int id = (int)action;
        uint mods = binding.Modifiers | (noRepeat ? Win32.MOD_NOREPEAT : 0u);
        bool ok = Win32.RegisterHotKey(_windowHandle, id, mods, (uint)binding.VirtualKey);
        if (ok)
        {
            _registeredIds.Add(id);
        }
        else
        {
            failures.Add($"{label} ({binding.ToDisplayString()})");
        }
    }

    public void UnregisterAll()
    {
        foreach (int id in _registeredIds)
        {
            Win32.UnregisterHotKey(_windowHandle, id);
        }
        _registeredIds.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
    }
}
