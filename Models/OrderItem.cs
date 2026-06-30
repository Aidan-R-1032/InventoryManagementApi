using InventoryApi.Models;

namespace InventoryManagementApi.Models
{
    public class OrderItem
    // uses two foreign keys to define an many-to-many relationship between Order and Product
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPriceAtOrderTime { get; set; }
        
        // Foreign key and navigation to Order
        public int OrderId { get; set; }
        public Order Order { get; set; } = null;

        // Foreign key and navigation to Product
        public int ProductId { get; set; }
        public Product Product { get; set; } = null;
    }
}
