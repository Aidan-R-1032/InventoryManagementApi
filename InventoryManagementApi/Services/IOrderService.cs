using InventoryManagementApi.Models;

namespace InventoryManagementApi.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order> PlaceOrderAsync(string customerName, List<OrderItemRequest> items);
        Task<bool> CancelOrderAsync(int id);
    }

    public record OrderItemRequest(int ProductId, int Quantity);
}