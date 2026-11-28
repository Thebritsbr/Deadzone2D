# Google sign-in no Android — configurar o Client ID correto

O erro `401 invalid_client / flowName=GeneralOAuthFlow` acontece porque o Client ID usado é do tipo **Desktop app**. O botão de login do Android (Google Identity Services) exige um Client ID do tipo **Aplicação Web**.

## 1. Criar o Client ID Web

1. Abra https://console.cloud.google.com/apis/credentials no mesmo projeto usado pelo PC.
2. Clique em **Criar credenciais → ID do cliente OAuth**.
3. Tipo: **Aplicação Web**.
4. Nome: `Deadzone2D Mobile`.
5. Em **Origens JavaScript autorizadas**, adicione:
   - `https://thebritsbr.github.io`
   - `http://localhost`
   - `https://localhost`
   - `capacitor://localhost`
   - `http://capacitor://localhost`
6. Copie o **Client ID** gerado (não é segredo).

## 2. Configurar no servidor (Render)

No painel do Render → serviço `deadzone2d` → **Environment**, adicione:

- Chave: `GOOGLE_WEB_CLIENT_ID`
- Valor: o Client ID copiado acima

Salve e aguarde o redeploy. O servidor passa a devolver esse ID em `GET /api/config`, e o app mobile usa automaticamente.

## 3. Verificar

Abra `https://deadzone2d.onrender.com/api/config`. Deve retornar:

```json
{ "googleClientId": "SEU-ID.apps.googleusercontent.com", "serverVersion": "4.6.5" }
```

Depois reinstale o APK e use o botão do Google.

Se não quiser configurar agora, o login com **Gmail + senha** continua funcionando normalmente.
