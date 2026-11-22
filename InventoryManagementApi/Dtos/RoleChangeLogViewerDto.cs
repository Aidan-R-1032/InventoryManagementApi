using InventoryManagementApi.Models;

namespace InventoryManagementApi.Dtos
{
    public class RoleChangeLogViewerDto
    {
        public int Id { get; set; }
        public int PromoterId { get; set; }
        public string PromoterUsername { get; set; } = string.Empty;
        public int TargetUserId { get; set; }
        public string TargetUsername { get; set; } = string.Empty;
        public UserRole OldRole { get; set; }
        public UserRole NewRole { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string IPAddress { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}
