using System;
using System.Drawing;
using System.Windows.Forms;
using BiliGamePiP.Models;
using BiliGamePiP.Native;

namespace BiliGamePiP.Forms;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _working;

    private readonly TextBox _txtSeekBack;
    private readonly TextBox _txtPlayPause;
    private readonly TextBox _txtSeekForward;
    private readonly TextBox _txtClickThrough;
    private readonly TextBox _txtMiniMode;
    private readonly NumericUpDown _numSeekSeconds;
    private readonly TrackBar _trackOpacity;
    private readonly Label _lblOpacityValue;

    public AppSettings ResultSettings => _working;

    public SettingsForm(AppSettings current)
    {
        _working = current.Clone();

        Text = "快捷键与小窗设置 - BiliGamePiP";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(24, 24, 32);
        ForeColor = Color.FromArgb(238, 238, 245);
        Font = new Font("Microsoft YaHei UI", 9.5f);
        ClientSize = new Size(470, 415);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 16, 18, 14),
            ColumnCount = 2,
            RowCount = 9
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));

        for (int i = 0; i < 7; i++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38f));
        }
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));

        _txtSeekBack = CreateHotkeyBox(() => _working.SeekBackward, v => _working.SeekBackward = v);
        _txtPlayPause = CreateHotkeyBox(() => _working.PlayPause, v => _working.PlayPause = v);
        _txtSeekForward = CreateHotkeyBox(() => _working.SeekForward, v => _working.SeekForward = v);
        _txtClickThrough = CreateHotkeyBox(() => _working.ToggleClickThrough, v => _working.ToggleClickThrough = v);
        _txtMiniMode = CreateHotkeyBox(() => _working.ToggleMiniMode, v => _working.ToggleMiniMode = v);

        AddRow(root, 0, "后退（快退）快捷键：", _txtSeekBack);
        AddRow(root, 1, "暂停 / 播放快捷键：", _txtPlayPause);
        AddRow(root, 2, "快进快捷键：", _txtSeekForward);
        AddRow(root, 3, "鼠标穿透锁定 / 解锁：", _txtClickThrough);
        AddRow(root, 4, "切换置顶小窗模式：", _txtMiniMode);

        _numSeekSeconds = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 60,
            Value = Math.Clamp(_working.SeekSeconds, 1, 60),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(38, 38, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 5, 0, 5)
        };
        AddRow(root, 5, "每次快进 / 后退秒数：", _numSeekSeconds);

        var opacityPanel = new Panel { Dock = DockStyle.Fill };
        _lblOpacityValue = new Label
        {
            Text = $"{(int)Math.Round(_working.MiniOpacity * 100)}%",
            Dock = DockStyle.Right,
            Width = 46,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(200, 200, 215)
        };
        _trackOpacity = new TrackBar
        {
            Minimum = 40,
            Maximum = 100,
            TickFrequency = 10,
            Value = Math.Clamp((int)Math.Round(_working.MiniOpacity * 100), 40, 100),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(24, 24, 32)
        };
        _trackOpacity.ValueChanged += (_, _) =>
        {
            _lblOpacityValue.Text = $"{_trackOpacity.Value}%";
        };
        opacityPanel.Controls.Add(_trackOpacity);
        opacityPanel.Controls.Add(_lblOpacityValue);
        AddRow(root, 6, "小窗不透明度：", opacityPanel);

        var tipLabel = new Label
        {
            Text = "提示：点击输入框后直接按下想要的按键即可录制（按 Backspace 清空）。\r\n" +
                   "使用小键盘数字键（Num0~Num9）时请保持 NumLock 开启；\r\n" +
                   "小键盘 7/8/9 与原神、终末地主键盘 1-4 切人完全独立，互不冲突。",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(156, 160, 178),
            Font = new Font("Microsoft YaHei UI", 8.5f),
            Padding = new Padding(0, 8, 0, 4)
        };
        root.Controls.Add(tipLabel, 0, 7);
        root.SetColumnSpan(tipLabel, 2);

        var btnBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 6, 0, 0)
        };

        var btnSave = CreateActionButton("保存并应用", Color.FromArgb(251, 114, 153), Color.White);
        btnSave.Click += (_, _) =>
        {
            _working.SeekSeconds = (int)_numSeekSeconds.Value;
            _working.MiniOpacity = _trackOpacity.Value / 100.0;
            DialogResult = DialogResult.OK;
            Close();
        };

        var btnCancel = CreateActionButton("取消", Color.FromArgb(52, 52, 66), Color.White);
        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var btnReset = CreateActionButton("恢复默认", Color.FromArgb(44, 44, 56), Color.FromArgb(220, 220, 232));
        btnReset.Click += (_, _) =>
        {
            var def = new AppSettings();
            _working.SeekBackward = def.SeekBackward;
            _working.PlayPause = def.PlayPause;
            _working.SeekForward = def.SeekForward;
            _working.ToggleClickThrough = def.ToggleClickThrough;
            _working.ToggleMiniMode = def.ToggleMiniMode;
            _working.SeekSeconds = def.SeekSeconds;
            _working.MiniOpacity = def.MiniOpacity;
            RefreshFields();
        };

        btnBar.Controls.Add(btnSave);
        btnBar.Controls.Add(btnCancel);
        btnBar.Controls.Add(btnReset);

        root.Controls.Add(btnBar, 0, 8);
        root.SetColumnSpan(btnBar, 2);

        Controls.Add(root);
        RefreshFields();
    }

    private void RefreshFields()
    {
        _txtSeekBack.Text = _working.SeekBackward.ToDisplayString();
        _txtPlayPause.Text = _working.PlayPause.ToDisplayString();
        _txtSeekForward.Text = _working.SeekForward.ToDisplayString();
        _txtClickThrough.Text = _working.ToggleClickThrough.ToDisplayString();
        _txtMiniMode.Text = _working.ToggleMiniMode.ToDisplayString();
        _numSeekSeconds.Value = Math.Clamp(_working.SeekSeconds, 1, 60);
        _trackOpacity.Value = Math.Clamp((int)Math.Round(_working.MiniOpacity * 100), 40, 100);
        _lblOpacityValue.Text = $"{_trackOpacity.Value}%";
    }

    private static void AddRow(TableLayoutPanel panel, int row, string labelText, Control control)
    {
        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(226, 226, 236)
        };
        panel.Controls.Add(label, 0, row);
        panel.Controls.Add(control, 1, row);
    }

    private TextBox CreateHotkeyBox(Func<HotkeyBinding> getter, Action<HotkeyBinding> setter)
    {
        var box = new TextBox
        {
            ReadOnly = true,
            Dock = DockStyle.Fill,
            TextAlign = HorizontalAlignment.Center,
            BackColor = Color.FromArgb(38, 38, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 5, 0, 5),
            Cursor = Cursors.Hand
        };

        box.Enter += (_, _) =>
        {
            box.BackColor = Color.FromArgb(58, 42, 62);
            box.Text = "请按下新快捷键...";
        };

        box.Leave += (_, _) =>
        {
            box.BackColor = Color.FromArgb(38, 38, 50);
            box.Text = getter().ToDisplayString();
        };

        box.KeyDown += (_, e) =>
        {
            e.SuppressKeyPress = true;
            e.Handled = true;

            if (e.KeyCode == Keys.Escape)
            {
                box.Text = getter().ToDisplayString();
                SelectNextControl(box, true, true, true, true);
                return;
            }

            if (e.KeyCode is Keys.Back or Keys.Delete)
            {
                setter(new HotkeyBinding());
                box.Text = "未设置";
                return;
            }

            if (e.KeyCode is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin)
            {
                return;
            }

            uint mods = Win32.MOD_NONE;
            if (e.Control) mods |= Win32.MOD_CONTROL;
            if (e.Alt) mods |= Win32.MOD_ALT;
            if (e.Shift) mods |= Win32.MOD_SHIFT;

            var binding = new HotkeyBinding(mods, e.KeyCode);
            setter(binding);
            box.Text = binding.ToDisplayString();
        };

        return box;
    }

    private static Button CreateActionButton(string text, Color back, Color fore)
    {
        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(92, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = fore,
            Margin = new Padding(8, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }
}
