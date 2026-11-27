using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BiliGamePiP.Native;

namespace BiliGamePiP.Models;

public sealed class HotkeyBinding
{
    public uint Modifiers { get; set; }
    public int VirtualKey { get; set; }

    public HotkeyBinding()
    {
    }

    public HotkeyBinding(uint modifiers, Keys key)
    {
        Modifiers = modifiers;
        VirtualKey = (int)key;
    }

    public bool IsValid => VirtualKey > 0;

    public HotkeyBinding Clone() => new()
    {
        Modifiers = Modifiers,
        VirtualKey = VirtualKey
    };

    public string ToDisplayString()
    {
        if (!IsValid)
        {
            return "未设置";
        }

        var parts = new List<string>();
        if ((Modifiers & Win32.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((Modifiers & Win32.MOD_ALT) != 0) parts.Add("Alt");
        if ((Modifiers & Win32.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((Modifiers & Win32.MOD_WIN) != 0) parts.Add("Win");
        parts.Add(FormatKeyName((Keys)VirtualKey));
        return string.Join(" + ", parts);
    }

    public static string FormatKeyName(Keys key) => key switch
    {
        Keys.NumPad0 => "Num0",
        Keys.NumPad1 => "Num1",
        Keys.NumPad2 => "Num2",
        Keys.NumPad3 => "Num3",
        Keys.NumPad4 => "Num4",
        Keys.NumPad5 => "Num5",
        Keys.NumPad6 => "Num6",
        Keys.NumPad7 => "Num7",
        Keys.NumPad8 => "Num8",
        Keys.NumPad9 => "Num9",
        Keys.Decimal => "Num.",
        Keys.Add => "Num+",
        Keys.Subtract => "Num-",
        Keys.Multiply => "Num*",
        Keys.Divide => "Num/",
        Keys.Left => "←",
        Keys.Right => "→",
        Keys.Up => "↑",
        Keys.Down => "↓",
        Keys.Space => "Space",
        Keys.Oemcomma => ",",
        Keys.OemPeriod => ".",
        Keys.OemOpenBrackets => "[",
        Keys.OemCloseBrackets => "]",
        Keys.OemMinus => "-",
        Keys.Oemplus => "=",
        _ => key.ToString()
    };
}

public sealed class AppSettings
{
    // 默认避开主键盘与小键盘 1-4，采用小键盘 7/8/9 控制播放，0 锁定穿透，小数点切换小窗
    public HotkeyBinding SeekBackward { get; set; } = new(0, Keys.NumPad7);
    public HotkeyBinding PlayPause { get; set; } = new(0, Keys.NumPad8);
    public HotkeyBinding SeekForward { get; set; } = new(0, Keys.NumPad9);
    public HotkeyBinding ToggleClickThrough { get; set; } = new(0, Keys.NumPad0);
    public HotkeyBinding ToggleMiniMode { get; set; } = new(0, Keys.Decimal);

    public int SeekSeconds { get; set; } = 5;
    public double MiniOpacity { get; set; } = 1.0;

    public int MiniX { get; set; } = -1;
    public int MiniY { get; set; } = -1;
    public int MiniWidth { get; set; } = 480;
    public int MiniHeight { get; set; } = 270;

    public string LastUrl { get; set; } = "https://www.bilibili.com";

    public AppSettings Clone() => new()
    {
        SeekBackward = SeekBackward.Clone(),
        PlayPause = PlayPause.Clone(),
        SeekForward = SeekForward.Clone(),
        ToggleClickThrough = ToggleClickThrough.Clone(),
        ToggleMiniMode = ToggleMiniMode.Clone(),
        SeekSeconds = SeekSeconds,
        MiniOpacity = MiniOpacity,
        MiniX = MiniX,
        MiniY = MiniY,
        MiniWidth = MiniWidth,
        MiniHeight = MiniHeight,
        LastUrl = LastUrl
    };
}
