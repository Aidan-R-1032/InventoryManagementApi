using InventoryManagementApi.Models;
using Org.BouncyCastle.Asn1.Mozilla;

namespace InventoryManagementApi.Dtos
{
    public class RoleChangeLogQueryDto
    {
        public int? Page { get; set; } = 1;
        public int? Amount { get; set; } = 20;
        public UserRole? NewRole { get; set; } 
        public UserRole? OldRole { get; set; }
        public DateTime? Begin { get; set; }
        public DateTime? End { get; set; }
        public int? PromoterId { get; set; }
        public int? TargetUserId { get; set; }
    }
}
