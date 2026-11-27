using System;
using System.Threading;
using System.Windows.Forms;
using BiliGamePiP.Forms;

namespace BiliGamePiP;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Global\BiliGamePiP_SingleInstance_v1", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "BiliGamePiP 已经在运行中，请查看右下角系统托盘图标。",
                "BiliGamePiP",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
