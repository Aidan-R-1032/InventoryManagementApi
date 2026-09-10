namespace InventoryManagementApi.Models
{
    public enum UserRole
    {
        Admin,
        Staff
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Staff;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}