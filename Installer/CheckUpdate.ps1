# CheckUpdate.ps1 — Verifica atualizações no GitHub Pages e instala a versão mais recente.
$ErrorActionPreference = 'SilentlyContinue'
$url = 'https://thebritsbr.github.io/Deadzone2D/update.json'
$local = '1.8'

try {
  $data = Invoke-RestMethod -Uri $url -TimeoutSec 15
  $remote = [string]$data.Version
  if ([version]$remote -gt [version]$local) {
    $pkg = $data.PackageUrl
    if (-not $pkg) { exit 0 }
    $tmp = Join-Path $env:TEMP ([System.IO.Path]::GetFileName([uri]$pkg).LocalPath)
    Invoke-WebRequest -Uri $pkg -OutFile $tmp -TimeoutSec 300
    if (Test-Path $tmp) {
      Start-Process -FilePath $tmp -ArgumentList '/VERYSILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS'
    }
  }
} catch { }
exit 0
