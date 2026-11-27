using System;
using System.Collections.Generic;
using System.Windows.Forms;
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

/// <summary>
/// 使用独立的隐藏 NativeWindow 接收 WM_HOTKEY，彻底避免 MainForm 切换 FormBorderStyle
/// 导致句柄重建 (RecreateHandle) 时全局快捷键全部失效的问题。
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private readonly HotkeyMessageWindow _messageWindow;
    private readonly HashSet<int> _registeredIds = new();

    public event Action<HotkeyAction>? HotkeyPressed;

    public HotkeyManager()
    {
        _messageWindow = new HotkeyMessageWindow(action => HotkeyPressed?.Invoke(action));
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
        bool ok = Win32.RegisterHotKey(_messageWindow.Handle, id, mods, (uint)binding.VirtualKey);
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
        if (_messageWindow.Handle != IntPtr.Zero)
        {
            foreach (int id in _registeredIds)
            {
                Win32.UnregisterHotKey(_messageWindow.Handle, id);
            }
        }
        _registeredIds.Clear();
    }

    public void Dispose()
    {
        UnregisterAll();
        _messageWindow.DestroyHandle();
    }

    private sealed class HotkeyMessageWindow : NativeWindow
    {
        private readonly Action<HotkeyAction> _onHotkey;

        public HotkeyMessageWindow(Action<HotkeyAction> onHotkey)
        {
            _onHotkey = onHotkey;
            CreateHandle(new CreateParams
            {
                Caption = "BiliGamePiP_HotkeySink"
            });
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32.WM_HOTKEY)
            {
                var action = (HotkeyAction)m.WParam.ToInt32();
                _onHotkey(action);
                return;
            }

            base.WndProc(ref m);
        }
    }
}
