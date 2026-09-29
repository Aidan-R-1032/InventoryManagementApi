using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace InventoryManagementApi.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string username, string resetToken)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                    _configuration["Email:FromName"],
                    _configuration["Email:FromAddress"]
                ));

            message.To.Add(new MailboxAddress(username, toEmail));
            message.Subject = "Password Reset Request";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Password Reset Request</h2>
                    <p>Hi {username},</p>
                    <p>You requested a password reset for your account.</p>
                    <p>Use the token below to reset your password. It expires in 15 minutes.</p>

                    <p>
                        <strong>Reset Token:</strong><br/>
                        <code style=""background:#f4f4f4;padding:8px;display:block;margin:8px 0;"">
                            {resetToken}
                        </code>
                    </p>

                    <p>
                        POST the following to reset your password:<br/>
                        <code>POST /api/auth/reset-password</code><br/>
                        <code>{{
                            ""token"": ""{resetToken}"",
                            ""newPassword"": ""<your new password>""
                        }}</code>
                    </p>

                    <p>If you did not request this reset, ignore this email. Your password will not change.</p>
                    <p>This token expires in 15 minutes.</p>
                "
            };
            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(
                        _configuration["Email:SmtpHost"],
                        int.Parse(_configuration["Email:SmtpPort"]),
                        SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(
                        _configuration["Email:Username"],
                        _configuration["Email:Password"]
                    );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Password reset email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendAccountDeleteEmailAsync(string toEmail, string username, string deleteToken)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                    _configuration["Email:FromName"],
                    _configuration["Email:FromAddress"]
                ));

            message.To.Add(new MailboxAddress(username, toEmail));
            message.Subject = "Account deletion Request";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Account Deletion Request</h2>
                    <p>Hi {username},</p>
                    <p>You requested for your account to be deleted.</p>
                    <p>Use the token below to confirm the deletion request. It expires in 15 minutes.</p>

                    <p>
                        <strong>Deletion Token:</strong><br/>
                        <code style=""background:#f4f4f4;padding:8px;display:block;margin:8px 0;"">
                            {deleteToken}
                        </code>
                    </p>

                    <p>
                        POST the following to delete your account:<br/>
                        <code>POST /api/auth/confirm-delete</code><br/>
                        <code>{{
                            ""token"": ""{deleteToken}"",
                        }}</code>
                    </p>

                    <p>If you did not request this reset, ignore this email. Your account will still be active.</p>
                    <p>This token expires in 15 minutes.</p>
                "
            };

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(
                        _configuration["Email:SmtpHost"],
                        int.Parse(_configuration["Email:SmtpPort"]),
                        SecureSocketOptions.StartTls
                    );

                await client.AuthenticateAsync(
                        _configuration["Email:Username"],
                        _configuration["Email:Password"]
                    );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Account deletion email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send account deletion email to {Email}", toEmail);
                throw;
            }
        }
    }
}
