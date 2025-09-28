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

        public async Task SendGroupJoinRequestNotificationAsync(string email, string groupName, string requesterId)
        {
            var subject = $"グループ「{groupName}」への参加申請が届きました";
            var message = $@"
                <h2>グループ参加申請通知</h2>
                <p>グループ「{groupName}」への参加申請が届きました。</p>
                <p>申請者ID: {requesterId}</p>
                <p>アプリケーションにログインして申請を確認してください。</p>
            ";

            await SendEmailAsync(email, subject, message);
        }

        public async Task SendGroupJoinApprovalNotificationAsync(string email, string groupName, bool approved)
        {
            var subject = approved ? 
                $"グループ「{groupName}」への参加が承認されました" : 
                $"グループ「{groupName}」への参加申請が拒否されました";
            
            var message = approved ?
                $@"
                    <h2>グループ参加承認通知</h2>
                    <p>グループ「{groupName}」への参加が承認されました。</p>
                    <p>アプリケーションにログインしてグループ機能をご利用ください。</p>
                " :
                $@"
                    <h2>グループ参加拒否通知</h2>
                    <p>グループ「{groupName}」への参加申請が拒否されました。</p>
                    <p>詳細についてはグループ管理者にお問い合わせください。</p>
                ";

            await SendEmailAsync(email, subject, message);
        }
    }
}
