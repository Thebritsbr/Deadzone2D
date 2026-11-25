// Program.cs — Entry point do Crossfire Win (sem Engine)
using CrossfireWin;

ApplicationConfiguration.Initialize();
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);

var form = new MainForm();
if (args.Length > 0 && args[0].Equals("--play", StringComparison.OrdinalIgnoreCase))
    form.QuickPlay(args.Skip(1).ToArray());
UpdateService.ApplyPending();
Application.Run(new SplashForm());
Application.Run(form);
