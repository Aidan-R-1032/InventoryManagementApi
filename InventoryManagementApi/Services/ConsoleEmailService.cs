namespace InventoryManagementApi.Services
{
    public class ConsoleEmailService : IEmailService
    {
        private readonly ILogger<ConsoleEmailService> _logger;
        
        public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendPasswordResetEmailAsync(string toEmail, string username, string resetToken)
        {
            _logger.LogInformation("====== PASSWORD RESET ======");
            _logger.LogInformation("To: {Email}", toEmail);
            _logger.LogInformation("Username: {Username}", username);
            _logger.LogInformation("Reset Token: {Token}", resetToken);
            _logger.LogInformation("Token expires in 15 minutes.");
            _logger.LogInformation("In production, this would send:");
            _logger.LogInformation("  POST /api/auth/reset-password");
            _logger.LogInformation("  {{ \"token\": \"{Token}\", \"newPassword\": \"<new password>\" }}", resetToken);
            _logger.LogInformation("==================================");

            return Task.CompletedTask;
        }
    }
}
