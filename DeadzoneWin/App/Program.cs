// Program.cs — Entry point do Deadzone2D (sem Engine)
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Deadzone2D;

[DllImport("user32.dll")]
static extern IntPtr SetForegroundWindow(IntPtr hwnd);
[DllImport("user32.dll")]
static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

ApplicationConfiguration.Initialize();
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

var form = new MainForm();
if (args.Length > 0 && args[0].Equals("--play", StringComparison.OrdinalIgnoreCase))
    form.QuickPlay(args.Skip(1).ToArray());

// Se já existir uma instância do jogo, focar a janela existente e sair.
foreach (var p in System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly()!.Location)))
{
    if (p.Id != System.Diagnostics.Process.GetCurrentProcess().Id && p.MainWindowHandle != IntPtr.Zero)
    {
        SetForegroundWindow(p.MainWindowHandle);
        const uint WM_SHOWWINDOW = 0x0018;
        PostMessage(p.MainWindowHandle, WM_SHOWWINDOW, (IntPtr)1, IntPtr.Zero);
        form.Dispose();
        return;
    }
}

// Só a instância principal aplica a atualização pendente.
UpdateService.ApplyPending();

Application.Run(new SplashForm());
Application.Run(form);
