# Crossfire — Plano de Implementação

Este projeto implementa o jogo **Crossfire** no estilo **Unreal Engine**,
construído com **C++** (principal) e utilitários auxiliares em **C#**
(instalação + ferramentas).

## Stack principal (Unreal Engine 5)

| Sistema | Caminho | Descrição |
|--------|---------|-----------|
| Jogo C++ | `UnrealProject/Source/Crossfire/` | Modules Runtime comGameplay, Systems, UI |
| Launch | `Crossfire.uproject` | Projeto engines (C++ module `Crossfire`) |
| Configuração | `UnrealProject/Config/` | `DefaultEngine.ini`, `GameModes.json`, `Customize.json`, `UpdateConfig.json` |
| Armas | `Assets/Guns/` | `guns.json`, Glonk/Mp40/SniperAWM/Ak47/MiniUZI + skins |
| Texturas | `Assets/Textures/Skins/` | Default, AmberRoyal, RustyTactical, NeonFury |
| Instalador | `Installer/Setup.iss` | Inno Setup, coloca build em `dist/` |

## Sistemas

1. **FUpdateManager** (`Systems/FUpdateManager.h/.cpp`) — consulta `UpdateConfig.json`, compara versão e entrega notificação de update. O update ocorre antes do primeiro login.
2. **FNativeWindowManager** (`Systems/FNativeWindowManager.h/.cpp`) — janela personalizada (borderless) com `SGameBar` no topo.
3. **SGameBar** (`UI/SGameBar.h/.cpp`) — barra de título com botões de minimizar / maximizar / fechar, styled via `Customize.json`.
4. **SLoadingScreen** (`UI/SLoadingScreen.h/.cpp`) — tela de carregamento estilo tático (spinners animados, barra).
5. **SLoginScreen** (`UI/SLoginScreen.h/.cpp`) — botões Google / Convidado.
6. **SLobbyScreen** (`UI/SLobbyScreen.h/.cpp`) — Lobby estilo Free Fire: lista de jogadores com avatares.
7. **SFriendsManager** (`UI/SFriendsManager.h/.cpp`) — amizades com nome + ícone; detalhes (ID, data de criação, último login).
8. **FWeaponsDataManager** (`UI/FWeaponsDataManager.h/.cpp`) — lê `Guns.json` e arquivos de arma; `SWeaponsPanel` mostra armas por categoria + skins.
9. **FGameNetworkManager** (`Systems/FGameNetworkManager.h/.cpp`) — login guest/Google + lobby + sessões.
10. **FGameModesManager** (`Systems/FGameModesManager.h/.cpp`) — lê `GameModes.json` (modos, limites de jogadores).
11. **Installer** (`Installer/Setup.iss`) — instalador Inno Setup + `build.ps1` → `dist/`.

## GameModes.json

Estrutura para configurar os modos (Limites de jogador, times, tempo, rodadas, etc.):

```json
{
  "Modes": [
    { "Id": "death_match", "MaxPlayers": 12, "TeamsEnabled": false, ... },
    { "Id": "team_death_match", "MaxPlayers": 12, "TeamCount": 2, "TeamSize": 6, ... },
    { "Id": "battle_royale", "MaxPlayers": 100, ... }
  ],
  "DefaultModeId": "battle_royale",
  "MaxPlayersPerTeam": 50
}
```

## Next steps

1. Compilar o projeto no Unreal Engine 5.6.
2. Exportar modelos reais (FBX) para `Assets/Guns/Models/`.
3. Rodar `Installer/build.ps1` → `dist/CrossfireSetup_v01.00.00.exe`.
4. Fazer upload do instalador para `UpdateConfig.json` (`DownloadUrl`).
