// Program.cs — Entry point do Deadzone2D (sem Engine)
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Deadzone2D;

[DllImport("user32.dll")]
static extern IntPtr SetForegroundWindow(IntPtr hwnd);
[DllImport("user32.dll")]
static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

ApplicationConfiguration.Initialize();
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

// Instância única: evita duas janelas de login/jogo quando o instalador e o
// atualizador tentam abrir o jogo ao mesmo tempo.
using var mutex = new Mutex(true, @"Local\Deadzone2D_SingleInstance", out bool firstInstance);
if (!firstInstance)
{
    FocusExistingInstance();
    return;
}

var form = new MainForm();
if (args.Length > 0 && args[0].Equals("--play", StringComparison.OrdinalIgnoreCase))
    form.QuickPlay(args.Skip(1).ToArray());

// Só a instância principal aplica a atualização pendente.
UpdateService.ApplyPending();

Application.Run(new SplashForm());
Application.Run(form);

try { mutex.ReleaseMutex(); } catch { }

static void FocusExistingInstance()
{
    for (int i = 0; i < 25; i++)
    {
        var p = Process.GetProcessesByName("Deadzone2DApp")
            .FirstOrDefault(x => x.Id != Environment.ProcessId && x.MainWindowHandle != IntPtr.Zero);
        if (p != null)
        {
            SetForegroundWindow(p.MainWindowHandle);
            const uint WM_SHOWWINDOW = 0x0018;
            PostMessage(p.MainWindowHandle, WM_SHOWWINDOW, (IntPtr)1, IntPtr.Zero);
            return;
        }
        Thread.Sleep(100);
    }
}
