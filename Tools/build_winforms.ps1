# build_winforms.ps1 — Monta o app WinForms com dotnet
cd "C:\Users\pc\OneDrive\Desktop\Deadzone\DeadzoneWin\App"
dotnet build -c Release | Out-String | Select-Object -First 15
