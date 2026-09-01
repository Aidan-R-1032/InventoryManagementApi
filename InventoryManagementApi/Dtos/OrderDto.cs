namespace InventoryManagementApi.Dtos
{
    public record OrderItemRequestDto(int ProductId, int Quantity);

    public record CreateOrderDto(
        string CustomerName,
        List<OrderItemRequestDto> Items);

    public record OrderItemResponseDto(
        int ProductId,
        string ProductName,
        int Quantity,
        decimal UnitPriceAtTimeOfOrder);

    public record OrderResponseDto(
        int Id,
        string CustomerName,
        DateTime OrderDate,
        string Status,
        List<OrderItemResponseDto> Items,
        decimal TotalAmount);
}