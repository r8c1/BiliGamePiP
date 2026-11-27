using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using BiliGamePiP.Controls;
using BiliGamePiP.Models;
using BiliGamePiP.Native;
using BiliGamePiP.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BiliGamePiP.Forms;

public sealed class MainForm : Form
{
    private const int ResizeGripSize = 6;

    private AppSettings _settings;
    private HotkeyManager? _hotkeyManager;
    private UpdateInfo? _availableUpdate;

    private readonly Panel _normalToolbar;
    private readonly TechAddressBar _addressBar;
    private readonly TechButton _btnUpdate;
    private readonly Panel _miniDragBar;
    private readonly Label _miniHintLabel;
    private readonly WebView2 _webView;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _trayToggleMiniItem;
    private readonly ToolStripMenuItem _trayToggleClickThroughItem;

    private bool _webViewReady;
    private bool _isMiniMode;
    private bool _isClickThrough;
    private Rectangle _normalBounds = new(120, 80, 1240, 790);
    private FormWindowState _normalWindowState = FormWindowState.Normal;

    public MainForm()
    {
        _settings = SettingsStore.Load();

        Text = $"BiliGamePiP {UpdateService.CurrentVersionText} — B站游戏置顶小窗";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1240, 790);
        MinimumSize = new Size(280, 170);
        BackColor = TechTheme.BgWindow;
        ForeColor = TechTheme.TextPrimary;
        Font = new Font("Microsoft YaHei UI", 9.5f);
        AutoScaleMode = AutoScaleMode.Dpi;

        TryLoadAppIcon();

        // 1. 极简暗夜科技感顶部工具栏
        _normalToolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = TechTheme.BgWindow,
            Padding = new Padding(10, 9, 10, 9)
        };
        _normalToolbar.Paint += (_, e) =>
        {
            using var pen = new Pen(TechTheme.BorderSubtle, 1f);
            e.Graphics.DrawLine(pen, 0, _normalToolbar.Height - 1, _normalToolbar.Width, _normalToolbar.Height - 1);
        };

        var leftBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            Margin = Padding.Empty
        };

        var btnBack = new TechButton
        {
            Text = "‹",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            Size = new Size(34, 32),
            CornerRadius = 9,
            Margin = new Padding(0, 0, 6, 0)
        };
        btnBack.Click += (_, _) =>
        {
            var core = _webView.CoreWebView2;
            if (_webViewReady && core is not null && core.CanGoBack)
            {
                core.GoBack();
            }
        };

        var btnRefresh = new TechButton
        {
            Text = "↻",
            Font = new Font("Segoe UI Symbol", 10.5f),
            Size = new Size(34, 32),
            CornerRadius = 9,
            Margin = new Padding(0, 0, 6, 0)
        };
        btnRefresh.Click += (_, _) =>
        {
            var core = _webView.CoreWebView2;
            if (_webViewReady && core is not null)
            {
                core.Reload();
            }
        };

        var btnHome = new TechButton
        {
            Text = "首页",
            Size = new Size(56, 32),
            CornerRadius = 9,
            Margin = new Padding(0, 0, 8, 0)
        };
        btnHome.Click += (_, _) => NavigateToInput("https://www.bilibili.com");

        leftBar.Controls.Add(btnBack);
        leftBar.Controls.Add(btnRefresh);
        leftBar.Controls.Add(btnHome);

        var rightBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            Margin = Padding.Empty
        };

        _btnUpdate = new TechButton
        {
            Text = "↑ 有新版本",
            Variant = TechButtonVariant.CyanAccent,
            Size = new Size(98, 32),
            CornerRadius = 16,
            Margin = new Padding(8, 0, 0, 0),
            Visible = false
        };
        _btnUpdate.Click += (_, _) =>
        {
            if (_availableUpdate is not null)
            {
                ShowUpdateDialog(_availableUpdate);
            }
        };

        var btnMini = new TechButton
        {
            Text = "📌 小窗置顶",
            Variant = TechButtonVariant.PrimaryGradient,
            Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Bold),
            Size = new Size(106, 32),
            CornerRadius = 16,
            Margin = new Padding(8, 0, 0, 0)
        };
        btnMini.Click += (_, _) => SetMiniMode(true);

        var btnSettings = new TechButton
        {
            Text = "⚙ 设置",
            Size = new Size(76, 32),
            CornerRadius = 16,
            Margin = new Padding(8, 0, 0, 0)
        };
        btnSettings.Click += (_, _) => OpenSettingsDialog();

        rightBar.Controls.Add(_btnUpdate);
        rightBar.Controls.Add(btnMini);
        rightBar.Controls.Add(btnSettings);

        var urlContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(4, 0, 4, 0)
        };
        _addressBar = new TechAddressBar
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "输入 B 站链接或 BV 号，回车直达..."
        };
        _addressBar.Submitted += (_, input) => NavigateToInput(input);
        urlContainer.Controls.Add(_addressBar);

        _normalToolbar.Controls.Add(urlContainer);
        _normalToolbar.Controls.Add(leftBar);
        _normalToolbar.Controls.Add(rightBar);

        // 2. 小窗置顶模式顶部极简科技感拖拽条（开启鼠标穿透时自动隐藏）
        _miniDragBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 30,
            BackColor = Color.FromArgb(16, 18, 28),
            Visible = false,
            Cursor = Cursors.SizeAll,
            Padding = new Padding(0, 2, 0, 0)
        };
        _miniDragBar.Paint += (_, e) =>
        {
            if (_miniDragBar.Width <= 0) return;
            var lineRect = new Rectangle(0, 0, _miniDragBar.Width, 2);
            using var brush = new LinearGradientBrush(lineRect, TechTheme.AccentPink, TechTheme.AccentCyan, 0f);
            e.Graphics.FillRectangle(brush, lineRect);
        };
        _miniDragBar.MouseDown += OnMiniBarMouseDown;

        _miniHintLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = TechTheme.TextSecondary,
            Font = new Font("Consolas", 8.2f),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Cursor = Cursors.SizeAll
        };
        _miniHintLabel.MouseDown += OnMiniBarMouseDown;

        var miniButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(0, 3, 4, 3)
        };

        var btnLock = new TechButton
        {
            Text = "🔒 穿透",
            Variant = TechButtonVariant.CyanAccent,
            Font = new Font("Microsoft YaHei UI", 8f),
            Size = new Size(60, 22),
            CornerRadius = 6,
            Margin = new Padding(4, 0, 0, 0)
        };
        btnLock.Click += (_, _) => SetClickThrough(true);

        var btnExitMini = new TechButton
        {
            Text = "⛶ 选片",
            Variant = TechButtonVariant.PrimaryGradient,
            Font = new Font("Microsoft YaHei UI", 8f),
            Size = new Size(58, 22),
            CornerRadius = 6,
            Margin = new Padding(4, 0, 0, 0)
        };
        btnExitMini.Click += (_, _) => SetMiniMode(false);

        var btnHideToTray = new TechButton
        {
            Text = "—",
            Font = new Font("Microsoft YaHei UI", 8f),
            Size = new Size(26, 22),
            CornerRadius = 6,
            Margin = new Padding(4, 0, 0, 0)
        };
        btnHideToTray.Click += (_, _) => Hide();

        miniButtons.Controls.Add(btnLock);
        miniButtons.Controls.Add(btnExitMini);
        miniButtons.Controls.Add(btnHideToTray);

        _miniDragBar.Controls.Add(_miniHintLabel);
        _miniDragBar.Controls.Add(miniButtons);

        // 3. WebView2 浏览器控件
        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = TechTheme.BgWindow
        };

        Controls.Add(_webView);
        Controls.Add(_miniDragBar);
        Controls.Add(_normalToolbar);

        // 4. 暗黑风格系统托盘菜单
        var trayMenu = new ContextMenuStrip
        {
            Renderer = new DarkMenuRenderer(),
            ShowImageMargin = false,
            Font = new Font("Microsoft YaHei UI", 9f)
        };
        trayMenu.Items.Add("显示并置前窗口", null, (_, _) => RestoreAndFocus());
        _trayToggleMiniItem = new ToolStripMenuItem("进入置顶小窗", null, (_, _) => SetMiniMode(!_isMiniMode));
        trayMenu.Items.Add(_trayToggleMiniItem);
        _trayToggleClickThroughItem = new ToolStripMenuItem("锁定鼠标穿透", null, (_, _) => SetClickThrough(!_isClickThrough));
        trayMenu.Items.Add(_trayToggleClickThroughItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("暂停 / 播放", null, (_, _) => _ = ExecuteScriptSafeAsync(BiliScript.GetTogglePlayPauseScript()));
        trayMenu.Items.Add("偏好设置...", null, (_, _) => OpenSettingsDialog());
        trayMenu.Items.Add("检查更新...", null, async (_, _) => await CheckUpdatesManuallyAsync());
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add("退出 BiliGamePiP", null, (_, _) => Close());

        _trayIcon = new NotifyIcon
        {
            Icon = Icon ?? SystemIcons.Application,
            Text = $"BiliGamePiP {UpdateService.CurrentVersionText}",
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => RestoreAndFocus();

        UpdateMiniHintText();

        Load += async (_, _) => await InitializeAsync();
        FormClosing += OnFormClosing;
        ResizeEnd += (_, _) => SaveCurrentMiniBounds();
        Move += (_, _) =>
        {
            if (_isMiniMode && WindowState == FormWindowState.Normal)
            {
                SaveCurrentMiniBounds();
            }
        };
    }

    private void TryLoadAppIcon()
    {
        try
        {
            string icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(icoPath))
            {
                Icon = new Icon(icoPath);
            }
        }
        catch
        {
        }
    }

    private async Task InitializeAsync()
    {
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.HotkeyPressed += HandleGlobalHotkey;
        ApplyHotkeysWithWarning();

        try
        {
            Directory.CreateDirectory(SettingsStore.WebView2UserDataDirectory);
            var env = await CoreWebView2Environment.CreateAsync(null, SettingsStore.WebView2UserDataDirectory);
            await _webView.EnsureCoreWebView2Async(env);

            var core = _webView.CoreWebView2;
            core.Settings.IsStatusBarEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = true;
            core.Settings.AreDevToolsEnabled = false;

            core.NewWindowRequested += (_, e) =>
            {
                e.Handled = true;
                if (!string.IsNullOrWhiteSpace(e.Uri))
                {
                    core.Navigate(e.Uri);
                }
            };

            core.SourceChanged += (_, _) =>
            {
                string uri = core.Source ?? string.Empty;
                _addressBar.Text = uri;
                if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    _settings.LastUrl = uri;
                }
            };

            core.NavigationCompleted += async (_, _) =>
            {
                if (_isMiniMode)
                {
                    await ExecuteScriptSafeAsync(BiliScript.GetMiniModeScript(true));
                }
            };

            core.HistoryChanged += async (_, _) =>
            {
                if (_isMiniMode)
                {
                    await ExecuteScriptSafeAsync(BiliScript.GetMiniModeScript(true));
                }
            };

            _webViewReady = true;
            NavigateToInput(_settings.LastUrl);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            var res = MessageBox.Show(
                "未检测到 Microsoft Edge WebView2 运行时（Windows 10/11 通常已自带）。\n是否立即打开微软官方下载页面安装？",
                "缺少 WebView2 运行时",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://developer.microsoft.com/microsoft-edge/webview2/",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"初始化内置浏览器失败：{ex.Message}",
                "BiliGamePiP",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        // 启动后静默检查一次 GitHub 新版本
        _ = CheckUpdatesOnStartupAsync();
    }

    private async Task CheckUpdatesOnStartupAsync()
    {
        await Task.Delay(2500);
        var info = await UpdateService.CheckForUpdateAsync();
        if (info is null || IsDisposed) return;

        _availableUpdate = info;
        _btnUpdate.Text = $"↑ {info.TagName}";
        _btnUpdate.Visible = true;

        bool skipped = string.Equals(_settings.SkippedUpdateVersion, info.TagName, StringComparison.OrdinalIgnoreCase);
        if (!skipped && !_isClickThrough && Visible)
        {
            ShowUpdateDialog(info);
        }
    }

    private async Task<bool> CheckUpdatesManuallyAsync()
    {
        var info = await UpdateService.CheckForUpdateAsync();
        if (info is null)
        {
            MessageBox.Show(
                $"当前已是最新版本（{UpdateService.CurrentVersionText}）。",
                "检查更新 - BiliGamePiP",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        _availableUpdate = info;
        _btnUpdate.Text = $"↑ {info.TagName}";
        _btnUpdate.Visible = true;
        ShowUpdateDialog(info);
        return true;
    }

    private void ShowUpdateDialog(UpdateInfo info)
    {
        if (_isClickThrough)
        {
            SetClickThrough(false, showOsd: false);
        }

        bool prevTopMost = TopMost;
        TopMost = false;

        try
        {
            using var dialog = new UpdateForm(info);
            dialog.ShowDialog(this);

            if (dialog.SkipThisVersion)
            {
                _settings.SkippedUpdateVersion = info.TagName;
                SettingsStore.Save(_settings);
            }
            else if (dialog.LaunchInstallerAndExit)
            {
                Close();
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                TopMost = prevTopMost;
            }
        }
    }

    private void NavigateToInput(string rawInput)
    {
        var core = _webView.CoreWebView2;
        if (!_webViewReady || core is null) return;

        string text = (rawInput ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text))
        {
            text = "https://www.bilibili.com";
        }
        else if (text.StartsWith("BV", StringComparison.OrdinalIgnoreCase) && !text.Contains('/'))
        {
            text = $"https://www.bilibili.com/video/{text}";
        }
        else if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                 !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            text = "https://" + text;
        }

        try
        {
            core.Navigate(text);
        }
        catch
        {
            core.Navigate("https://www.bilibili.com");
        }
    }

    private void SetMiniMode(bool enable)
    {
        if (_isMiniMode == enable) return;

        if (enable)
        {
            _normalWindowState = WindowState;
            _normalBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;

            _isMiniMode = true;
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            Padding = new Padding(3);
            BackColor = TechTheme.BorderBright;

            _normalToolbar.Visible = false;
            _miniDragBar.Visible = !_isClickThrough;

            Bounds = GetSafeMiniBounds();
            Opacity = Math.Clamp(_settings.MiniOpacity, 0.35, 1.0);

            _trayToggleMiniItem.Text = "退出置顶小窗";
            _ = ExecuteScriptSafeAsync(BiliScript.GetMiniModeScript(true));
            _ = ExecuteScriptSafeAsync(BiliScript.GetShowOsdScript(
                $"📌 置顶小窗已开启 · {_settings.SeekBackward.ToDisplayString()}后退 / {_settings.PlayPause.ToDisplayString()}暂停 / {_settings.SeekForward.ToDisplayString()}快进"));
        }
        else
        {
            if (_isClickThrough)
            {
                SetClickThrough(false, showOsd: false);
            }

            SaveCurrentMiniBounds();
            _isMiniMode = false;

            Opacity = 1.0;
            TopMost = false;
            Padding = Padding.Empty;
            BackColor = TechTheme.BgWindow;
            FormBorderStyle = FormBorderStyle.Sizable;

            _miniDragBar.Visible = false;
            _normalToolbar.Visible = true;

            Bounds = _normalBounds;
            WindowState = _normalWindowState;

            _trayToggleMiniItem.Text = "进入置顶小窗";
            _ = ExecuteScriptSafeAsync(BiliScript.GetMiniModeScript(false));
        }

        SettingsStore.Save(_settings);
    }

    private void SetClickThrough(bool enable, bool showOsd = true)
    {
        if (enable && !_isMiniMode)
        {
            SetMiniMode(true);
        }

        if (_isClickThrough == enable) return;

        _isClickThrough = enable;
        if (enable)
        {
            _miniDragBar.Visible = false;
            Padding = Padding.Empty;
            Win32.SetClickThrough(Handle, true);
            _trayToggleClickThroughItem.Text = "解锁鼠标穿透";
            if (showOsd)
            {
                string keyName = _settings.ToggleClickThrough.ToDisplayString();
                _ = ExecuteScriptSafeAsync(BiliScript.GetShowOsdScript($"🔒 已锁定鼠标穿透（按 {keyName} 解锁）"));
            }
        }
        else
        {
            Win32.SetClickThrough(Handle, false);
            if (_isMiniMode)
            {
                _miniDragBar.Visible = true;
                Padding = new Padding(3);
            }
            _trayToggleClickThroughItem.Text = "锁定鼠标穿透";
            if (showOsd)
            {
                _ = ExecuteScriptSafeAsync(BiliScript.GetShowOsdScript("🔓 已解锁小窗，可拖动或调整大小"));
            }
        }
    }

    private Rectangle GetSafeMiniBounds()
    {
        int width = Math.Clamp(_settings.MiniWidth, 280, 1920);
        int height = Math.Clamp(_settings.MiniHeight, 160, 1080);

        Rectangle area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        int x = _settings.MiniX;
        int y = _settings.MiniY;

        if (x < 0 || y < 0)
        {
            x = area.Right - width - 24;
            y = area.Top + 24;
        }
        else
        {
            var candidate = new Rectangle(x, y, width, height);
            bool visibleOnAnyScreen = false;
            foreach (var scr in Screen.AllScreens)
            {
                if (scr.WorkingArea.IntersectsWith(candidate))
                {
                    visibleOnAnyScreen = true;
                    break;
                }
            }
            if (!visibleOnAnyScreen)
            {
                x = area.Right - width - 24;
                y = area.Top + 24;
            }
        }

        return new Rectangle(x, y, width, height);
    }

    private void SaveCurrentMiniBounds()
    {
        if (!_isMiniMode || WindowState != FormWindowState.Normal) return;

        _settings.MiniX = Left;
        _settings.MiniY = Top;
        _settings.MiniWidth = Width;
        _settings.MiniHeight = Height;
    }

    private void RestoreAndFocus()
    {
        if (!Visible)
        {
            Show();
        }
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }
        if (_isClickThrough)
        {
            SetClickThrough(false);
        }
        Activate();
    }

    private void OpenSettingsDialog()
    {
        if (_isClickThrough)
        {
            SetClickThrough(false, showOsd: false);
        }
        if (!Visible)
        {
            Show();
        }

        _hotkeyManager?.UnregisterAll();

        bool prevTopMost = TopMost;
        TopMost = false;

        try
        {
            using var dialog = new SettingsForm(_settings, async () =>
            {
                var info = await UpdateService.CheckForUpdateAsync();
                if (info is null) return false;
                _availableUpdate = info;
                _btnUpdate.Text = $"↑ {info.TagName}";
                _btnUpdate.Visible = true;
                ShowUpdateDialog(info);
                return true;
            });

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _settings = dialog.ResultSettings;
                SettingsStore.Save(_settings);
                UpdateMiniHintText();
                if (_isMiniMode)
                {
                    Opacity = Math.Clamp(_settings.MiniOpacity, 0.35, 1.0);
                }
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                TopMost = prevTopMost;
                ApplyHotkeysWithWarning();
            }
        }
    }

    private void ApplyHotkeysWithWarning()
    {
        if (_hotkeyManager is null) return;

        var failures = _hotkeyManager.Apply(_settings);
        if (failures.Count > 0)
        {
            MessageBox.Show(
                "以下快捷键已被其他软件占用，注册失败：\n" +
                string.Join("\n", failures) +
                "\n\n请点击「⚙ 设置」更换为其他按键。",
                "快捷键冲突提示",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void UpdateMiniHintText()
    {
        _miniHintLabel.Text =
            $"{_settings.SeekBackward.ToDisplayString()}后退 · " +
            $"{_settings.PlayPause.ToDisplayString()}暂停 · " +
            $"{_settings.SeekForward.ToDisplayString()}快进 · " +
            $"{_settings.ToggleClickThrough.ToDisplayString()}穿透";
    }

    private async Task ExecuteScriptSafeAsync(string script)
    {
        var core = _webView.CoreWebView2;
        if (!_webViewReady || core is null) return;

        try
        {
            await core.ExecuteScriptAsync(script);
        }
        catch
        {
        }
    }

    private void OnMiniBarMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _isMiniMode && !_isClickThrough)
        {
            Win32.BeginWindowDrag(Handle);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Win32.ApplyDarkWindowChrome(Handle, TechTheme.BgWindow, TechTheme.BorderSubtle, roundedCorners: true);
        if (_isClickThrough)
        {
            Win32.SetClickThrough(Handle, true);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Win32.WM_NCHITTEST && _isMiniMode && !_isClickThrough)
        {
            base.WndProc(ref m);
            long lParam = m.LParam.ToInt64();
            int screenX = (short)(lParam & 0xFFFF);
            int screenY = (short)((lParam >> 16) & 0xFFFF);
            Point pt = PointToClient(new Point(screenX, screenY));

            bool left = pt.X <= ResizeGripSize;
            bool right = pt.X >= ClientSize.Width - ResizeGripSize;
            bool top = pt.Y <= ResizeGripSize;
            bool bottom = pt.Y >= ClientSize.Height - ResizeGripSize;

            if (top && left) { m.Result = (IntPtr)Win32.HTTOPLEFT; return; }
            if (top && right) { m.Result = (IntPtr)Win32.HTTOPRIGHT; return; }
            if (bottom && left) { m.Result = (IntPtr)Win32.HTBOTTOMLEFT; return; }
            if (bottom && right) { m.Result = (IntPtr)Win32.HTBOTTOMRIGHT; return; }
            if (left) { m.Result = (IntPtr)Win32.HTLEFT; return; }
            if (right) { m.Result = (IntPtr)Win32.HTRIGHT; return; }
            if (top) { m.Result = (IntPtr)Win32.HTTOP; return; }
            if (bottom) { m.Result = (IntPtr)Win32.HTBOTTOM; return; }
            return;
        }

        base.WndProc(ref m);
    }

    private void HandleGlobalHotkey(HotkeyAction action)
    {
        switch (action)
        {
            case HotkeyAction.SeekBackward:
                _ = ExecuteScriptSafeAsync(BiliScript.GetSeekScript(-_settings.SeekSeconds));
                break;
            case HotkeyAction.PlayPause:
                _ = ExecuteScriptSafeAsync(BiliScript.GetTogglePlayPauseScript());
                break;
            case HotkeyAction.SeekForward:
                _ = ExecuteScriptSafeAsync(BiliScript.GetSeekScript(_settings.SeekSeconds));
                break;
            case HotkeyAction.ToggleClickThrough:
                SetClickThrough(!_isClickThrough);
                break;
            case HotkeyAction.ToggleMiniMode:
                SetMiniMode(!_isMiniMode);
                break;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveCurrentMiniBounds();
        SettingsStore.Save(_settings);

        _hotkeyManager?.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
    }
}
