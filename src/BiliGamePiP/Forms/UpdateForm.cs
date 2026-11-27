using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using BiliGamePiP.Controls;
using BiliGamePiP.Native;
using BiliGamePiP.Services;

namespace BiliGamePiP.Forms;

public sealed class UpdateForm : Form
{
    private readonly UpdateInfo _info;
    private readonly TechProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly TechButton _btnInstall;
    private readonly TechButton _btnBrowser;
    private readonly TechButton _btnSkip;

    private CancellationTokenSource? _cts;

    public bool SkipThisVersion { get; private set; }
    public bool LaunchInstallerAndExit { get; private set; }

    public UpdateForm(UpdateInfo info)
    {
        _info = info;

        Text = "发现新版本 - BiliGamePiP";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = TechTheme.BgWindow;
        ForeColor = TechTheme.TextPrimary;
        Font = new Font("Microsoft YaHei UI", 9.5f);
        ClientSize = new Size(490, 360);

        var root = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18)
        };

        var header = new Label
        {
            Text = $"✨ 发现新版本 {_info.TagName}",
            Font = new Font("Microsoft YaHei UI", 13.5f, FontStyle.Bold),
            ForeColor = TechTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(22, 18)
        };

        string sizeHint = _info.InstallerSizeBytes > 0
            ? $" · 安装包 {_info.InstallerSizeBytes / (1024.0 * 1024.0):F1} MB"
            : string.Empty;

        var subHeader = new Label
        {
            Text = $"当前版本 {UpdateService.CurrentVersionText}  →  最新版本 {_info.TagName}{sizeHint}",
            Font = new Font("Microsoft YaHei UI", 9f),
            ForeColor = TechTheme.AccentCyan,
            AutoSize = true,
            Location = new Point(24, 48)
        };

        var notesCard = new TechCardPanel
        {
            Location = new Point(22, 78),
            Size = new Size(446, 168),
            Padding = new Padding(12)
        };

        var notesBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = TechTheme.BgSurface,
            ForeColor = TechTheme.TextSecondary,
            Font = new Font("Microsoft YaHei UI", 9f),
            Dock = DockStyle.Fill,
            Text = string.IsNullOrWhiteSpace(_info.ReleaseNotes)
                ? "本次更新包含功能优化与问题修复，建议立即升级。"
                : _info.ReleaseNotes.Replace("\n", "\r\n")
        };
        notesCard.Controls.Add(notesBox);

        _statusLabel = new Label
        {
            Text = "点击「一键下载并安装」，将自动下载安装包并启动升级向导。",
            Font = new Font("Microsoft YaHei UI", 8.6f),
            ForeColor = TechTheme.TextSecondary,
            Location = new Point(22, 256),
            Size = new Size(446, 20)
        };

        _progressBar = new TechProgressBar
        {
            Location = new Point(22, 280),
            Size = new Size(446, 10),
            Visible = false
        };

        _btnInstall = new TechButton
        {
            Text = "⬇ 一键下载并安装",
            Variant = TechButtonVariant.PrimaryGradient,
            Size = new Size(156, 36),
            Location = new Point(312, 304),
            Enabled = !string.IsNullOrWhiteSpace(_info.InstallerDownloadUrl)
        };
        _btnInstall.Click += async (_, _) => await StartDownloadAndInstallAsync();

        _btnBrowser = new TechButton
        {
            Text = "浏览器下载",
            Variant = TechButtonVariant.CyanAccent,
            Size = new Size(106, 36),
            Location = new Point(196, 304)
        };
        _btnBrowser.Click += (_, _) =>
        {
            OpenUrl(_info.ReleasePageUrl);
            Close();
        };

        _btnSkip = new TechButton
        {
            Text = "跳过此版本",
            Variant = TechButtonVariant.Ghost,
            Size = new Size(104, 36),
            Location = new Point(22, 304)
        };
        _btnSkip.Click += (_, _) =>
        {
            SkipThisVersion = true;
            Close();
        };

        root.Controls.Add(header);
        root.Controls.Add(subHeader);
        root.Controls.Add(notesCard);
        root.Controls.Add(_statusLabel);
        root.Controls.Add(_progressBar);
        root.Controls.Add(_btnSkip);
        root.Controls.Add(_btnBrowser);
        root.Controls.Add(_btnInstall);
        Controls.Add(root);

        FormClosing += (_, _) => _cts?.Cancel();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Win32.ApplyDarkWindowChrome(Handle, TechTheme.BgWindow, TechTheme.BorderSubtle);
    }

    private async System.Threading.Tasks.Task StartDownloadAndInstallAsync()
    {
        _btnInstall.Enabled = false;
        _btnSkip.Enabled = false;
        _progressBar.Visible = true;
        _progressBar.Percent = 0;
        _statusLabel.ForeColor = TechTheme.AccentCyan;
        _statusLabel.Text = "正在连接 GitHub 下载安装包...";

        _cts = new CancellationTokenSource();
        var progress = new Progress<(long downloaded, long? total)>(p =>
        {
            double dlMb = p.downloaded / (1024.0 * 1024.0);
            if (p.total.HasValue && p.total.Value > 0)
            {
                int pct = (int)Math.Clamp(p.downloaded * 100L / p.total.Value, 0, 100);
                double totalMb = p.total.Value / (1024.0 * 1024.0);
                _progressBar.Percent = pct;
                _statusLabel.Text = $"正在下载安装包：{dlMb:F1} MB / {totalMb:F1} MB ({pct}%)";
            }
            else
            {
                _statusLabel.Text = $"正在下载安装包：{dlMb:F1} MB...";
            }
        });

        try
        {
            string installerPath = await UpdateService.DownloadInstallerAsync(_info, progress, _cts.Token);
            _progressBar.Percent = 100;
            _statusLabel.Text = "下载完成，正在启动安装向导...";

            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            });

            LaunchInstallerAndExit = true;
            Close();
        }
        catch (OperationCanceledException)
        {
            // User closed dialog
        }
        catch (Exception ex)
        {
            _btnInstall.Enabled = true;
            _btnSkip.Enabled = true;
            _statusLabel.ForeColor = TechTheme.AccentPink;
            _statusLabel.Text = $"下载失败：{ex.Message}（可点击「浏览器下载」手动升级）";
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
        }
    }
}
