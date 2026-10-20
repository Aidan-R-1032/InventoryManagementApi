using InventoryManagementApi.Models;

namespace InventoryManagementApi.Dtos
{
    public class RoleChangeLogQueryDto
    {
        public int? Amount { get; set; } = 20;
        public UserRole? NewRole { get; set; } 
        public DateTime? Begin { get; set; }
        public DateTime? End { get; set; }
        public int? PromoterId { get; set; }
        public int? TargetUserId { get; set; }
    }
}
