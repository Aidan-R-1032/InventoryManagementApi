using InventoryManagementApi.Models;

namespace InventoryManagementApi.Dtos
{
    public static class DtoMapper
    {
        public static ProductResponseDto ToProductResponse(Product product) => new(
            product.Id,
            product.Name,
            product.Sku,
            product.Price,
            product.StockQuantity);

        public static OrderResponseDto ToOrderResponse(Order order) => new(
            order.Id,
            order.CustomerName,
            order.OrderDate,
            order.Status.ToString(),
            order.OrderItems.Select(oi => new OrderItemResponseDto(
                oi.ProductId,
                oi.Product?.Name ?? "Unknown",                                  // fails gracefully instead of throwing error
                oi.Quantity,
                oi.UnitPriceAtOrderTime)).ToList(),
            order.OrderItems.Sum(oi => oi.Quantity * oi.UnitPriceAtOrderTime)); // computes total amount
    }
}