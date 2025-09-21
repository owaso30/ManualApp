using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace ManualApp.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(ILogger<EmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // 開発環境ではコンソールにログ出力
            _logger.LogInformation("=== メール送信 ===");
            _logger.LogInformation("宛先: {Email}", email);
            _logger.LogInformation("件名: {Subject}", subject);
            _logger.LogInformation("本文: {Message}", htmlMessage);
            _logger.LogInformation("==================");

            // 本番環境では実際のメール送信サービス（SendGrid、SMTP等）を使用
            // ここでは開発用のダミー実装
            return Task.CompletedTask;
        }
    }
}
