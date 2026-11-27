// MailService.cs — envio de código de verificação por Gmail (SMTP com senha de app).
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace Deadzone2D
{
    public sealed class MailConfig
    {
        public bool Enabled { get; set; }
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromName { get; set; } = "Deadzone2D";
    }

    public static class MailService
    {
        public static MailConfig Config { get; private set; } = new();

        public static void Load(string dir)
        {
            try
            {
                var p = Path.Combine(dir, "Config", "Mail.json");
                if (!File.Exists(p)) return;
                var cfg = JsonSerializer.Deserialize<MailConfig>(File.ReadAllText(p),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (cfg != null) Config = cfg;
            }
            catch { }
        }

        public static bool IsConfigured =>
            Config.Enabled &&
            !string.IsNullOrWhiteSpace(Config.User) &&
            !string.IsNullOrWhiteSpace(Config.Password);

        public static bool SendVerificationCode(string to, string code, out string error)
        {
            error = "";
            if (!IsConfigured)
            {
                error = "SMTP não configurado. Configure Config/Mail.json para envio real.";
                return false;
            }
            try
            {
                using var client = new SmtpClient(Config.Host, Config.Port)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(Config.User, Config.Password),
                    Timeout = 12000
                };
                using var msg = new MailMessage
                {
                    From = new MailAddress(Config.User, Config.FromName),
                    Subject = "Deadzone2D - código de verificação",
                    Body = "Seu código de verificação Deadzone2D é: " + code +
                           "\n\nEle expira em 5 minutos. Se você não solicitou, ignore este e-mail.",
                    IsBodyHtml = false
                };
                msg.To.Add(to);
                client.Send(msg);
                return true;
            }
            catch (Exception ex)
            {
                error = "Não foi possível enviar o e-mail: " + ex.Message;
                return false;
            }
        }
    }
}
