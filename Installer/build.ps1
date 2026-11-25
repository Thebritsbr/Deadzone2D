# Requires -Version 5.1
# Builds DeadzoneSetup_v1.1.exe a partir do output em dist\Deadzone
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -ErrorAction SilentlyContinue
& (Get-Command "C:\Program Files\Inno Setup 7\iscc.exe").Path "C:\Users\pc\OneDrive\Desktop\Deadzone\Installer\Setup.iss"
