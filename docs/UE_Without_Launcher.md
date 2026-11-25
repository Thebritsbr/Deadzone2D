# UE_Without_Launcher.md — Rodando Unreal Engine sem Epic Games Launcher

> Nota honesta: dia de hoje, eu não consegui verificar os URLs do GitHub da Epic aqui
> (`https://github.com/EpicGames/UnrealEngine` retorna 404 neste ambiente), então o guia
> de download precisa ser validado no GitHub antes de seguir adiante.

## Pré-requisitos (Windows)

1. **Visual Studio Community (ou Pro) 2022 ou 2025** com as cargas:
   - "Game development with C++" (atenção: requer de pouca diferença em relação ao Community).
   - C++ CMake tools (opcional, mas útil).
   - C++ clang-format support.
   - English language packs (inglês).
   Instale com o **VS Installer** — https://visualstudio.microsoft.com/downloads/

2. Ferramentas extras que o UE instala pelo `GenerateProjectFiles`:
   - FiraSans (font files — https://fonts.adobe.com/fonts/fira-sans ou baixar via ferramenta do UE).

3. Espaço em disco: 30–50GB livres para build.

## Passos (fonte do GitHub)

1. Baixe o source do Unreal Engine:
   - Clonar/ZIP: `https://github.com/EpicGames/UnrealEngine`
   - Escolha o branch estável atual (ex.: `release_5.6` ou o mais novo; CONFIRMAR no GitHub).
2. Abrir o pastel: `cd UnrealEngine`.
3. Rodar `GenerateProjectFiles.bat` (gera os arquivos .sln/.vcxproj).
4. Build: `Build.bat` ou abrir a .sln no Visual Studio → Build.
5. Rodar o projeto: abrir `SOLUTION.sln` → rodar (ex.: "UnrealEditor").

## Depois, no projeto do jogo (Crossfire)

1. Setar a variável de ambiente `UE_ROOT` para a pasta do Unreal Editor.
2. Rodar `npm run build` no `Installer/build.ps1` (`.\Installer\build.ps1`) → gera `dist/CrossfireSetup_v1.1.exe`.

## Script de verificação de pré-requisitos

`Tools/check_ue_prereqs.ps1` — verifica Visual Studio, espaço em disco e lista itens faltantes:

```powershell
.\Tools\check_ue_prereqs.ps1
```
