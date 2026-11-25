#!/usr/bin/env pwsh
# check_ue_prereqs.ps1 — Verifica pré-requisitos para rodar Unreal Engine sem launcher
Write-Host "[UE] Verificando pré-requisitos..."

$totalIssues = 0

# 1) Visual Studio Community com "Game development with C++"
$VSpaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat",
    "C:\Program Files\Microsoft Visual Studio\2025\Community\VC\Auxiliary\Build\vcvars64.bat",
    "C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\vcvars64.bat"
)
$VS = $VSpaths | ForEach-Object { Get-Item -Path $_ -ErrorAction SilentlyContinue } | Select-Object -First 1
if ($VS) { Write-Host "[OK] Visual Studio/BuildTools: $($VS.FullName)" } else {
    Write-Host "[FAIL] Visual Studio não encontrado (procure vcvars64.bat em Program Files(x86)\Microsoft Visual Studio)"; $totalIssues++
}

# 2) Espaço em disco livre
$Drive = (Get-Item -Path "C:\" -ErrorAction SilentlyContinue).PSDrive
if ($Drive) {
    $FreeGB = [Math]::Round($Drive.Free/1GB, 1)
    if ($FreeGB -gt 50) { Write-Host "[OK] Espaço em disco: $FreeGB GB" } else {
        Write-Host "[WARN] Espaço em disco: $FreeGB GB (recomendado 30-50GB+)"; $totalIssues++
    }
}

# 3) Windows SDK
$SDK = Get-Item -Path "C:\Program Files (x86)\Windows Kits\10\Include\10.0.*" -ErrorAction SilentlyContinue
if ($SDK) { Write-Host "[OK] Windows SDK: $($SDK.Name)" } else {
    Write-Host "[FAIL] Windows SDK não encontrado em C:\Program Files (x86)\Windows Kits\10"; $totalIssues++
}

Write-Host ""
if ($totalIssues -eq 0) { Write-Host "[UE] Tudo parece OK!" }
else { Write-Host "[UE] $totalIssues item(ns) precisam atenção." }
