using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using BiliGamePiP.Forms;
using BiliGamePiP.Models;
using BiliGamePiP.Services;

namespace BiliGamePiP;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        bool isSmokeTest = args.Any(a => string.Equals(a, "--smoke-test", StringComparison.OrdinalIgnoreCase));

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportCrash(e.Exception, isSmokeTest);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                ReportCrash(ex, isSmokeTest);
            }
        };

        try
        {
            ApplicationConfiguration.Initialize();

            if (isSmokeTest)
            {
                return RunSmokeTest();
            }

            using var mutex = new Mutex(true, @"Local\BiliGamePiP_SingleInstance_v1", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    "BiliGamePiP 已经在运行中，请查看右下角系统托盘图标。",
                    "BiliGamePiP",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return 0;
            }

            Application.Run(new MainForm());
            return 0;
        }
        catch (Exception ex)
        {
            ReportCrash(ex, isSmokeTest);
            return 1;
        }
    }

    private static int RunSmokeTest()
    {
        using var main = new MainForm();
        main.CreateControl();

        using var settings = new SettingsForm(new AppSettings());
        settings.CreateControl();

        using var update = new UpdateForm(new UpdateInfo
        {
            TagName = "v9.9.9",
            Version = new Version(9, 9, 9),
            ReleasePageUrl = "https://github.com/r8c1/BiliGamePiP/releases",
            ReleaseNotes = "Smoke test"
        });
        update.CreateControl();

        return 0;
    }

    private static void ReportCrash(Exception ex, bool isSmokeTest)
    {
        string logPath = Path.Combine(SettingsStore.AppDataDirectory, "crash.log");
        try
        {
            Directory.CreateDirectory(SettingsStore.AppDataDirectory);
            File.AppendAllText(
                logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {UpdateService.CurrentVersionText}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
        }

        if (isSmokeTest)
        {
            Console.Error.WriteLine(ex.ToString());
            return;
        }

        try
        {
            MessageBox.Show(
                $"BiliGamePiP 启动或运行遇到异常：\n\n{ex.GetType().Name}: {ex.Message}\n\n详细日志已保存至：\n{logPath}",
                "BiliGamePiP 错误提示",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
        }
    }
}
