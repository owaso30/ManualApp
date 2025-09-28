using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ManualApp.Services
{
    public class ProductionEmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        public ProductionEmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {

                using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
                client.EnableSsl = _emailSettings.EnableSsl;
                client.Credentials = new NetworkCredential(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                throw;
            }
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

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}
