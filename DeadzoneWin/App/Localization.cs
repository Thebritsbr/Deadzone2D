// Localization.cs — idiomas da interface (en-US, pt-BR, es-ES).
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Deadzone2D
{
    public static class Loc
    {
        public static readonly string[] Languages = { "en-US", "pt-BR", "es-ES" };
        public static string Language { get; private set; } = "en-US";

        public static string Path => System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Deadzone2D", "language.json");

        private static readonly Dictionary<string, string> Pt = new()
        {
            ["SIGN IN"] = "ENTRAR", ["PASSWORD"] = "SENHA", ["GMAIL CODE"] = "CÓDIGO DO GMAIL",
            ["SEND CODE"] = "ENVIAR CÓDIGO", ["SIGN IN WITH GOOGLE"] = "ENTRAR COM GOOGLE",
            ["CREATE LOCAL ACCOUNT"] = "CRIAR CONTA LOCAL", ["CREATE ACCOUNT"] = "CRIAR CONTA",
            ["NEW PLAYER"] = "NOVO JOGADOR", ["BACK"] = "VOLTAR", ["CLOSE"] = "FECHAR",
            ["LOBBY"] = "LOBBY", ["WEAPONS"] = "ARMAS", ["ANNOUNCEMENTS"] = "ANÚNCIOS",
            ["VAULT"] = "COFRE", ["DEAD ROYAL"] = "DEAD ROYAL", ["MENU"] = "MENU",
            ["START"] = "INICIAR", ["LEAVE GROUP"] = "SAIR DO GRUPO", ["CREATE GROUP"] = "CRIAR GRUPO",
            ["INVITE"] = "CONVIDAR", ["LEAVE"] = "SAIR", ["TEAM MEMBERS"] = "MEMBROS DA EQUIPE",
            ["LEADER"] = "LÍDER", ["MEMBER"] = "MEMBRO", ["READY"] = "PRONTO",
            ["FRIENDS"] = "AMIGOS", ["ADD FRIEND"] = "ADICIONAR AMIGO", ["REQUESTS"] = "PEDIDOS",
            ["ACCEPT"] = "ACEITAR", ["DECLINE"] = "RECUSAR", ["NO FRIENDS YET"] = "NENHUM AMIGO AINDA",
            ["NO PENDING REQUESTS"] = "NENHUM PEDIDO PENDENTE", ["GROUP CREATED"] = "GRUPO CRIADO",
            ["STORE"] = "LOJA", ["LOADING MATCH"] = "CARREGANDO PARTIDA", ["ENTERING GAME"] = "ENTRANDO NO JOGO",
            ["RETURNING TO LOBBY"] = "VOLTANDO AO LOBBY", ["SETTINGS"] = "CONFIGURAÇÕES",
            ["CONFIGURATION"] = "CONFIGURAÇÕES", ["VOLUME"] = "VOLUME", ["LANGUAGE"] = "IDIOMA",
            ["PERFORMANCE MODE"] = "MODO OTIMIZADO", ["ON"] = "LIGADO", ["OFF"] = "DESLIGADO",
            ["VERSION"] = "VERSÃO", ["CHECK UPDATE"] = "VER ATUALIZAÇÃO", ["ACCOUNT"] = "CONTA",
            ["SAVE LANGUAGE"] = "SALVAR IDIOMA", ["CONFIRM LANGUAGE CHANGE"] = "CONFIRMAR MUDAÇA DE IDIOMA",
            ["CANCEL"] = "CANCELAR", ["REVERT"] = "REVERTER", ["CONFIRM"] = "CONFIRMAR",
            ["UPDATE AVAILABLE"] = "ATUALIZAÇÃO DISPONÍVEL", ["NEW VERSION"] = "NOVA VERSÃO",
            ["NO REINSTALL"] = "SEM REINSTALAR", ["PROFILE"] = "PERFIL", ["DESCRIPTION"] = "DESCRIÇÃO",
            ["REGION"] = "REGIÃO", ["LEVEL"] = "NÍVEL", ["RANK"] = "RANK", ["WINS"] = "VITÓRIAS",
            ["KILLS"] = "ABATES", ["EQUIP"] = "EQUIPAR", ["BUY"] = "COMPRAR",
            ["NOTIFICATIONS"] = "NOTIFICAÇÕES", ["NO NOTIFICATIONS"] = "NENHUMA NOTIFICAÇÃO",
            ["MORE"] = "MAIS", ["NAMES"] = "NOMES", ["ICON"] = "ÍCONE", ["BANNER"] = "BANNER"
        };

        private static readonly Dictionary<string, string> Es = new()
        {
            ["SIGN IN"] = "INICIAR SESIÓN", ["PASSWORD"] = "CONTRASEÑA", ["GMAIL CODE"] = "CÓDIGO DE GMAIL",
            ["SEND CODE"] = "ENVIAR CÓDIGO", ["SIGN IN WITH GOOGLE"] = "INICIAR CON GOOGLE",
            ["CREATE LOCAL ACCOUNT"] = "CREAR CUENTA LOCAL", ["CREATE ACCOUNT"] = "CREAR CUENTA",
            ["NEW PLAYER"] = "NUEVO JUGADOR", ["BACK"] = "VOLVER", ["CLOSE"] = "CERRAR",
            ["WEAPONS"] = "ARMAS", ["ANNOUNCEMENTS"] = "ANUNCIOS", ["VAULT"] = "CAJA",
            ["START"] = "INICIAR", ["LEAVE GROUP"] = "SALIR DEL GRUPO", ["CREATE GROUP"] = "CREAR GRUPO",
            ["INVITE"] = "INVITAR", ["LEAVE"] = "SALIR", ["TEAM MEMBERS"] = "MIEMBROS DEL EQUIPO",
            ["LEADER"] = "LÍDER", ["MEMBER"] = "MIEMBRO", ["READY"] = "LISTO",
            ["FRIENDS"] = "AMIGOS", ["ADD FRIEND"] = "AÑADIR AMIGO", ["REQUESTS"] = "SOLICITUDES",
            ["ACCEPT"] = "ACEPTAR", ["DECLINE"] = "RECHAZAR", ["NO FRIENDS YET"] = "SIN AMIGOS",
            ["NO PENDING REQUESTS"] = "NO HAY SOLICITUDES", ["GROUP CREATED"] = "GRUPO CREADO",
            ["STORE"] = "TIENDA", ["LOADING MATCH"] = "CARGANDO PARTIDA", ["ENTERING GAME"] = "ENTRANDO AL JUEGO",
            ["RETURNING TO LOBBY"] = "VOLVIENDO AL LOBBY", ["SETTINGS"] = "AJUSTES",
            ["CONFIGURATION"] = "CONFIGURACIÓN", ["VOLUME"] = "VOLUMEN", ["LANGUAGE"] = "IDIOMA",
            ["PERFORMANCE MODE"] = "MODO RENDIMIENTO", ["ON"] = "ACTIVADO", ["OFF"] = "DESACTIVADO",
            ["VERSION"] = "VERSIÓN", ["CHECK UPDATE"] = "BUSCAR ACTUALIZACIÓN", ["ACCOUNT"] = "CUENTA",
            ["SAVE LANGUAGE"] = "GUARDAR IDIOMA", ["CONFIRM LANGUAGE CHANGE"] = "CONFIRMAR CAMBIO DE IDIOMA",
            ["CANCEL"] = "CANCELAR", ["REVERT"] = "REVERTIR", ["CONFIRM"] = "CONFIRMAR",
            ["UPDATE AVAILABLE"] = "ACTUALIZACIÓN DISPONIBLE", ["NEW VERSION"] = "NUEVA VERSIÓN",
            ["NO REINSTALL"] = "SIN REINSTALAR", ["PROFILE"] = "PERFIL", ["DESCRIPTION"] = "DESCRIPCIÓN",
            ["REGION"] = "REGIÓN", ["LEVEL"] = "NIVEL", ["RANK"] = "RANGO", ["WINS"] = "VICTORIAS",
            ["KILLS"] = "BAJAS", ["EQUIP"] = "EQUIPAR", ["BUY"] = "COMPRAR",
            ["NOTIFICATIONS"] = "NOTIFICACIONES", ["NO NOTIFICATIONS"] = "SIN NOTIFICACIONES",
            ["MORE"] = "MÁS", ["NAMES"] = "NOMBRES", ["ICON"] = "ÍCONO", ["BANNER"] = "BANNER"
        };

        public static void Load()
        {
            try
            {
                if (!File.Exists(Path)) return;
                var doc = JsonDocument.Parse(File.ReadAllText(Path));
                var code = doc.RootElement.TryGetProperty("Language", out var v) ? v.GetString() : "en-US";
                if (Array.IndexOf(Languages, code) >= 0) Language = code!;
            }
            catch { }
        }

        public static void Save(string code)
        {
            if (Array.IndexOf(Languages, code) < 0) return;
            Language = code;
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                File.WriteAllText(Path, JsonSerializer.Serialize(new { Language = code }));
            }
            catch { }
        }

        public static string T(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var dict = Language == "pt-BR" ? Pt : Language == "es-ES" ? Es : null;
            if (dict != null && dict.TryGetValue(text, out var value)) return value;
            return text;
        }
    }
}
