using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiliGamePiP.Services;

public sealed class UpdateInfo
{
    public required string TagName { get; init; }
    public required Version Version { get; init; }
    public required string ReleasePageUrl { get; init; }
    public string? InstallerDownloadUrl { get; init; }
    public string? InstallerFileName { get; init; }
    public long InstallerSizeBytes { get; init; }
    public string ReleaseNotes { get; init; } = string.Empty;
}

public static class UpdateService
{
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/r8c1/BiliGamePiP/releases/latest";

    private static readonly HttpClient ApiClient = CreateClient(TimeSpan.FromSeconds(10));
    private static readonly HttpClient DownloadClient = CreateClient(Timeout.InfiniteTimeSpan);

    private static HttpClient CreateClient(TimeSpan timeout)
    {
        var client = new HttpClient { Timeout = timeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"BiliGamePiP/{CurrentVersionText}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public static Version CurrentVersion
    {
        get
        {
            var raw = Assembly.GetExecutingAssembly().GetName().Version;
            if (raw is null) return new Version(1, 2, 0);
            return new Version(raw.Major, raw.Minor, Math.Max(0, raw.Build));
        }
    }

    public static string CurrentVersionText => $"v{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";

    public static async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            using var resp = await ApiClient.GetAsync(LatestReleaseApiUrl, ct);
            if (!resp.IsSuccessStatusCode) return null;

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            string tag = root.TryGetProperty("tag_name", out var tagEl) ? (tagEl.GetString() ?? string.Empty) : string.Empty;
            if (!TryParseVersion(tag, out var latestVer)) return null;

            if (latestVer <= CurrentVersion)
            {
                return null;
            }

            string htmlUrl = root.TryGetProperty("html_url", out var urlEl)
                ? (urlEl.GetString() ?? "https://github.com/r8c1/BiliGamePiP/releases/latest")
                : "https://github.com/r8c1/BiliGamePiP/releases/latest";

            string body = root.TryGetProperty("body", out var bodyEl) ? (bodyEl.GetString() ?? string.Empty) : string.Empty;

            string? exeUrl = null;
            string? exeName = null;
            long exeSize = 0;

            if (root.TryGetProperty("assets", out var assetsEl) && assetsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsEl.EnumerateArray())
                {
                    string name = asset.TryGetProperty("name", out var n) ? (n.GetString() ?? string.Empty) : string.Empty;
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        exeName = name;
                        exeUrl = asset.TryGetProperty("browser_download_url", out var d) ? d.GetString() : null;
                        exeSize = asset.TryGetProperty("size", out var s) && s.TryGetInt64(out var bytes) ? bytes : 0;
                        break;
                    }
                }
            }

            return new UpdateInfo
            {
                TagName = tag,
                Version = latestVer,
                ReleasePageUrl = htmlUrl,
                InstallerDownloadUrl = exeUrl,
                InstallerFileName = exeName,
                InstallerSizeBytes = exeSize,
                ReleaseNotes = body.Trim()
            };
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string> DownloadInstallerAsync(
        UpdateInfo info,
        IProgress<(long downloaded, long? total)> progress,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(info.InstallerDownloadUrl))
        {
            throw new InvalidOperationException("当前 Release 未附带 .exe 安装包。");
        }

        string fileName = string.IsNullOrWhiteSpace(info.InstallerFileName)
            ? $"BiliGamePiP-Setup-{info.Version}.exe"
            : info.InstallerFileName;

        string targetDir = Path.Combine(Path.GetTempPath(), "BiliGamePiP_Updates");
        Directory.CreateDirectory(targetDir);
        string targetPath = Path.Combine(targetDir, fileName);
        string tempPath = targetPath + ".part";

        using var resp = await DownloadClient.GetAsync(info.InstallerDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        long? total = resp.Content.Headers.ContentLength ?? (info.InstallerSizeBytes > 0 ? info.InstallerSizeBytes : null);
        await using var src = await resp.Content.ReadAsStreamAsync(ct);
        await using (var dst = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            byte[] buffer = new byte[81920];
            long downloaded = 0;
            int read;
            while ((read = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), ct);
                downloaded += read;
                progress.Report((downloaded, total));
            }
        }

        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }
        File.Move(tempPath, targetPath);
        return targetPath;
    }

    private static bool TryParseVersion(string raw, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(raw)) return false;

        string cleaned = raw.Trim().TrimStart('v', 'V');
        int dashIdx = cleaned.IndexOf('-');
        if (dashIdx > 0)
        {
            cleaned = cleaned[..dashIdx];
        }

        if (!Version.TryParse(cleaned, out var parsed)) return false;
        version = new Version(parsed.Major, parsed.Minor, Math.Max(0, parsed.Build));
        return true;
    }
}
