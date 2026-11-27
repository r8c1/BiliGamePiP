using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using BiliGamePiP.Controls;
using BiliGamePiP.Models;
using BiliGamePiP.Native;
using BiliGamePiP.Services;

namespace BiliGamePiP.Forms;

public sealed class SettingsForm : Form
{
    private readonly AppSettings _working;
    private readonly Func<Task<bool>>? _onManualCheckUpdate;

    private readonly TextBox _txtSeekBack;
    private readonly TextBox _txtPlayPause;
    private readonly TextBox _txtSeekForward;
    private readonly TextBox _txtClickThrough;
    private readonly TextBox _txtMiniMode;
    private readonly Label _lblSeekValue;
    private readonly TechSlider _sliderOpacity;
    private readonly Label _lblOpacityValue;
    private readonly TechButton _btnCheckUpdate;
    private readonly Label _lblUpdateHint;

    public AppSettings ResultSettings => _working;

    public SettingsForm(AppSettings current, Func<Task<bool>>? onManualCheckUpdate = null)
    {
        _working = current.Clone();
        _onManualCheckUpdate = onManualCheckUpdate;

        Text = "偏好设置 - BiliGamePiP";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = TechTheme.BgWindow;
        ForeColor = TechTheme.TextPrimary;
        Font = new Font("Microsoft YaHei UI", 9.5f);
        ClientSize = new Size(512, 526);

        // 顶部标题与版本更新行
        var lblTitle = new Label
        {
            Text = "偏好设置",
            Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold),
            ForeColor = TechTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(22, 16)
        };

        _lblUpdateHint = new Label
        {
            Text = $"当前版本 {UpdateService.CurrentVersionText}",
            Font = new Font("Microsoft YaHei UI", 8.5f),
            ForeColor = TechTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(24, 46)
        };

        _btnCheckUpdate = new TechButton
        {
            Text = "↻ 检查更新",
            Variant = TechButtonVariant.CyanAccent,
            Size = new Size(104, 30),
            Location = new Point(386, 22),
            CornerRadius = 15
        };
        _btnCheckUpdate.Click += async (_, _) => await RunUpdateCheckAsync();

        // Card 1: 全局快捷键
        var hotkeyCard = new TechCardPanel
        {
            Location = new Point(22, 74),
            Size = new Size(468, 224),
            Padding = new Padding(16, 12, 16, 12)
        };

        var lblHotkeysTitle = new Label
        {
            Text = "全局快捷键（点击右侧按键框后直接按键盘录制）",
            Font = new Font("Microsoft YaHei UI", 8.8f, FontStyle.Bold),
            ForeColor = TechTheme.AccentPink,
            AutoSize = true,
            Location = new Point(16, 12)
        };
        hotkeyCard.Controls.Add(lblHotkeysTitle);

        _txtSeekBack = CreateHotkeyBox(() => _working.SeekBackward, v => _working.SeekBackward = v);
        _txtPlayPause = CreateHotkeyBox(() => _working.PlayPause, v => _working.PlayPause = v);
        _txtSeekForward = CreateHotkeyBox(() => _working.SeekForward, v => _working.SeekForward = v);
        _txtClickThrough = CreateHotkeyBox(() => _working.ToggleClickThrough, v => _working.ToggleClickThrough = v);
        _txtMiniMode = CreateHotkeyBox(() => _working.ToggleMiniMode, v => _working.ToggleMiniMode = v);

        AddHotkeyRow(hotkeyCard, 0, "后退（快退）", _txtSeekBack);
        AddHotkeyRow(hotkeyCard, 1, "暂停 / 播放", _txtPlayPause);
        AddHotkeyRow(hotkeyCard, 2, "快进", _txtSeekForward);
        AddHotkeyRow(hotkeyCard, 3, "鼠标穿透锁定 / 解锁", _txtClickThrough);
        AddHotkeyRow(hotkeyCard, 4, "切换普通选片 / 置顶小窗", _txtMiniMode);

        // Card 2: 播放与小窗体验
        var playCard = new TechCardPanel
        {
            Location = new Point(22, 310),
            Size = new Size(468, 114),
            Padding = new Padding(16, 12, 16, 12)
        };

        var lblPlayTitle = new Label
        {
            Text = "播放与小窗体验",
            Font = new Font("Microsoft YaHei UI", 8.8f, FontStyle.Bold),
            ForeColor = TechTheme.AccentCyan,
            AutoSize = true,
            Location = new Point(16, 12)
        };
        playCard.Controls.Add(lblPlayTitle);

        // 快进步长步进器
        var lblSeek = new Label
        {
            Text = "每次快进 / 后退步长",
            ForeColor = TechTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(16, 44)
        };

        var btnMinus = new TechButton
        {
            Text = "−",
            Size = new Size(30, 26),
            Location = new Point(308, 39),
            CornerRadius = 7
        };
        _lblSeekValue = new Label
        {
            Text = $"{_working.SeekSeconds} 秒",
            ForeColor = TechTheme.TextPrimary,
            Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(342, 39),
            Size = new Size(74, 26)
        };
        var btnPlus = new TechButton
        {
            Text = "+",
            Size = new Size(30, 26),
            Location = new Point(420, 39),
            CornerRadius = 7
        };
        btnMinus.Click += (_, _) =>
        {
            _working.SeekSeconds = Math.Clamp(_working.SeekSeconds - 1, 1, 60);
            _lblSeekValue.Text = $"{_working.SeekSeconds} 秒";
        };
        btnPlus.Click += (_, _) =>
        {
            _working.SeekSeconds = Math.Clamp(_working.SeekSeconds + 1, 1, 60);
            _lblSeekValue.Text = $"{_working.SeekSeconds} 秒";
        };

        playCard.Controls.Add(lblSeek);
        playCard.Controls.Add(btnMinus);
        playCard.Controls.Add(_lblSeekValue);
        playCard.Controls.Add(btnPlus);

        // 小窗透明度滑块
        var lblOpacity = new Label
        {
            Text = "小窗不透明度",
            ForeColor = TechTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(16, 80)
        };

        _sliderOpacity = new TechSlider
        {
            Minimum = 40,
            Maximum = 100,
            Value = Math.Clamp((int)Math.Round(_working.MiniOpacity * 100), 40, 100),
            Location = new Point(220, 74),
            Size = new Size(178, 28)
        };
        _lblOpacityValue = new Label
        {
            Text = $"{_sliderOpacity.Value}%",
            ForeColor = TechTheme.AccentCyan,
            Font = new Font("Consolas", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight,
            Location = new Point(402, 77),
            Size = new Size(48, 22)
        };
        _sliderOpacity.ValueChanged += (_, _) =>
        {
            _working.MiniOpacity = _sliderOpacity.Value / 100.0;
            _lblOpacityValue.Text = $"{_sliderOpacity.Value}%";
        };

        playCard.Controls.Add(lblOpacity);
        playCard.Controls.Add(_sliderOpacity);
        playCard.Controls.Add(_lblOpacityValue);

        // 底部提示与操作按钮
        var tipLabel = new Label
        {
            Text = "提示：按 Backspace 可清空热键；使用小键盘数字键请保持 NumLock 开启。",
            ForeColor = TechTheme.TextSecondary,
            Font = new Font("Microsoft YaHei UI", 8.3f),
            AutoSize = true,
            Location = new Point(24, 434)
        };

        var btnReset = new TechButton
        {
            Text = "恢复默认",
            Variant = TechButtonVariant.Ghost,
            Size = new Size(92, 36),
            Location = new Point(22, 468)
        };
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

        var btnCancel = new TechButton
        {
            Text = "取消",
            Variant = TechButtonVariant.Ghost,
            Size = new Size(88, 36),
            Location = new Point(276, 468)
        };
        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var btnSave = new TechButton
        {
            Text = "保存并应用",
            Variant = TechButtonVariant.PrimaryGradient,
            Size = new Size(116, 36),
            Location = new Point(374, 468)
        };
        btnSave.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.Add(lblTitle);
        Controls.Add(_lblUpdateHint);
        Controls.Add(_btnCheckUpdate);
        Controls.Add(hotkeyCard);
        Controls.Add(playCard);
        Controls.Add(tipLabel);
        Controls.Add(btnReset);
        Controls.Add(btnCancel);
        Controls.Add(btnSave);

        RefreshFields();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Win32.ApplyDarkWindowChrome(Handle, TechTheme.BgWindow, TechTheme.BorderSubtle);
    }

    private async Task RunUpdateCheckAsync()
    {
        _btnCheckUpdate.Enabled = false;
        _lblUpdateHint.ForeColor = TechTheme.AccentCyan;
        _lblUpdateHint.Text = "正在检查 GitHub 最新版本...";

        try
        {
            if (_onManualCheckUpdate is not null)
            {
                bool found = await _onManualCheckUpdate();
                _lblUpdateHint.ForeColor = found ? TechTheme.AccentPink : Color.FromArgb(74, 222, 128);
                _lblUpdateHint.Text = found
                    ? "已检测到新版本，请在更新窗口中升级。"
                    : $"✓ 已是最新版本 ({UpdateService.CurrentVersionText})";
            }
            else
            {
                var info = await UpdateService.CheckForUpdateAsync();
                if (info is null)
                {
                    _lblUpdateHint.ForeColor = Color.FromArgb(74, 222, 128);
                    _lblUpdateHint.Text = $"✓ 已是最新版本 ({UpdateService.CurrentVersionText})";
                }
                else
                {
                    _lblUpdateHint.ForeColor = TechTheme.AccentPink;
                    _lblUpdateHint.Text = $"发现新版本 {info.TagName}";
                }
            }
        }
        finally
        {
            _btnCheckUpdate.Enabled = true;
        }
    }

    private void RefreshFields()
    {
        _txtSeekBack.Text = _working.SeekBackward.ToDisplayString();
        _txtPlayPause.Text = _working.PlayPause.ToDisplayString();
        _txtSeekForward.Text = _working.SeekForward.ToDisplayString();
        _txtClickThrough.Text = _working.ToggleClickThrough.ToDisplayString();
        _txtMiniMode.Text = _working.ToggleMiniMode.ToDisplayString();
        _lblSeekValue.Text = $"{Math.Clamp(_working.SeekSeconds, 1, 60)} 秒";
        _sliderOpacity.Value = Math.Clamp((int)Math.Round(_working.MiniOpacity * 100), 40, 100);
        _lblOpacityValue.Text = $"{_sliderOpacity.Value}%";
    }

    private static void AddHotkeyRow(Control card, int index, string labelText, TextBox box)
    {
        int y = 40 + index * 35;
        var label = new Label
        {
            Text = labelText,
            ForeColor = TechTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(16, y + 4)
        };

        var wrap = new TechCardPanel
        {
            Location = new Point(268, y),
            Size = new Size(182, 28),
            CornerRadius = 8,
            CardBackColor = TechTheme.BgInput,
            CardBorderColor = TechTheme.BorderSubtle
        };
        box.Bounds = new Rectangle(8, 4, 166, 20);
        box.Enter += (_, _) =>
        {
            wrap.CardBorderColor = TechTheme.AccentPink;
            wrap.CardBackColor = Color.FromArgb(38, 24, 42);
            box.BackColor = Color.FromArgb(38, 24, 42);
            wrap.Invalidate();
        };
        box.Leave += (_, _) =>
        {
            wrap.CardBorderColor = TechTheme.BorderSubtle;
            wrap.CardBackColor = TechTheme.BgInput;
            box.BackColor = TechTheme.BgInput;
            wrap.Invalidate();
        };
        wrap.Controls.Add(box);

        card.Controls.Add(label);
        card.Controls.Add(wrap);
    }

    private TextBox CreateHotkeyBox(Func<HotkeyBinding> getter, Action<HotkeyBinding> setter)
    {
        var box = new TextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            TextAlign = HorizontalAlignment.Center,
            BackColor = TechTheme.BgInput,
            ForeColor = Color.FromArgb(255, 208, 223),
            Font = new Font("Consolas", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        box.Enter += (_, _) =>
        {
            box.ForeColor = Color.White;
            box.Text = "请按下新快捷键...";
        };

        box.Leave += (_, _) =>
        {
            box.ForeColor = Color.FromArgb(255, 208, 223);
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
}
