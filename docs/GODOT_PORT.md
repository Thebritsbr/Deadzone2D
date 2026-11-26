# Deadzone — Port para Godot 4

Este projeto adapta o jogo original ("Deadzone", mantido na versão Unreal) para
**Godot 4.4+**, novo projeto de baixa demanda — funciona no seu notebook.

## Rodando

1. Baixe o Godot 4.4+ **no site oficial do Godot**: https://godotengine.org (Já baixado é um arquivo .exe simples).
2. Abra o editor (projeto `GodotProject\project.godot`).
3. Pressione ▶ Play.

## Status do port

- [x] `project.godot` + `scenes/main.tscn`
- [x] Sistema de carregamento (`scripts/ui/LoadingScreen.gd`) — estilo tático
- [x] Login Google / Guest (`scripts/ui/LoginScreen.gd`)
- [x] Lobby estilo Free Fire (`scenes/LobbyScreen.tscn` + `scripts/ui/LobbyScreen.gd`)
- [x] Lista de amizadas com detalhes de ID/criação/último login (`scenes/FriendsPanel.tscn` + `scripts/ui/FriendsPanel.gd`)
- [x] Painel de Armas por categoria (`scenes/WeaponsPanel.tscn` + `scripts/ui/WeaponsPanel.gd`, usando `Assets/Guns/*.json` + skins)
- [x] Data de configuração de modos: `Config/GameModes.json` (lido por `scripts/systems/GameModesManager.gd`)
- [x] Sistema de update: `scripts/systems/UpdateManager.gd` (lê `Config/UpdateConfig.json`)
- [ ] Modelos 3D reais (`Assets/Guns/Models/*.obj` — placeholders atuais)
- [ ] Compartilhamento de rede/backend (stub)

## Estrutura

- `GodotProject/scripts/` — Game scripts GDScript
- `GodotProject/scenes/` — Sútons (.tscn)
- `GodotProject/Assets/` — Armas, skins, modelos (OBJ placeholders), texturas
- `GodotProject/Config/` — `GameModes.json`, `Customize.json`, `UpdateConfig.json`

## Exemplos

```bash
# Exporta pra distributar (standalone .exe, sem Launcher)
# Inno Setup (veja Installer/Setup.iss → Installer\build.ps1) → dist\DeadzoneSetup_v1.1.exe
```

## Dicas para personalizar

- Cores/atributos da barra de título: editar `Config/Customize.json`.
- Limites de jogadores por modo: editar `Config/GameModes.json`.
- Versão do jogo: editar `Config/UpdateConfig.json` (CurrentVersion/LatestVersion).

⚠️ Testes: o Godot editor é rodado local — não possível testar aqui.
