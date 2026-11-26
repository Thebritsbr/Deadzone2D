# Deadzone — Pasta de saídas (WinForms)

Aplicativo executável Windows (.NET 8), sem engine externa.

## Rodar a partir do `dist`

```
dist\Deadzone\Deadzone2DApp.exe
```

O config padrão carrega as colas: `Config\Maps.json`, `Config\Gameplay.json`,
`Config\Customize.json` e os coletes em `Assets\Guns`.

Modos:

- **Deal Duo** — 1v1 ou 2v2, vila abandonada, metas do jogo: 3v3 wins
  (placar `X`), empate exibido em cinza (`DEFEITO` em inglês, por config).
- **Squad Battle** — batalha de squad alguma, cidade abandonada.
- **Dead Royale** — 30 jogadores, contagem regressiva de 3:00, casas que
  contêm armas, moedas, itens e kit de cura.

Para usar o projeto fonte, realize `dotnet build` no
`DeadzoneWin\App\Deadzone2DApp.csproj` e execute a partir de `DeadzoneWin\App`
(o output também é copiado para `dist\Deadzone`).
