namespace InventoryManagementApi.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string username, string resetToken);
    }
}
