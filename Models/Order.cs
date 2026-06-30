namespace InventoryManagementApi.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // one order has many order items
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Canceled
    }
}
