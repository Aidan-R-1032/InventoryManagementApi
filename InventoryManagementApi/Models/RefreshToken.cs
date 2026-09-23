namespace InventoryManagementApi.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public User User { get; set; } = null;
        public DateTime ExpiresAt { get; set; }
        public bool isRevoked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? ReplacedByToken { get; set; }
            // when a refresh token is rotated, we store the token that replaces it
    }
}
