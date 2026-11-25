# Crossfire Win — Esqueleto do app WinForms (sem Engine)

C# + WinForms (via .NET). Roda no notebook atual.

## Rodar local
```powershell
cd DeadzoneWin\App
dotnet run
```

## Estrutura
- `CrossfireApp.csproj` — projeto WinForms
- `Program.cs` — entry point
- `MainForm.cs` — fluxo: Loading → Login → Lobby → Weapons
- `Config\` — GameModes.json, UpdateConfig.json, Customize.json
- `Assets\Guns\*.json` — armas (copiados)
