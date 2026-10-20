using InventoryManagementApi.Models;

namespace InventoryManagementApi.Dtos
{
    public class RoleChangeLogViewerDto
    {
        public int PromoterId { get; set; }
        public int TargetUserId { get; set; }
        public UserRole OldRole { get; set; }
        public UserRole NewRole { get; set; }
        public DateTime TimeStampUtc { get; set; }
    }
}
