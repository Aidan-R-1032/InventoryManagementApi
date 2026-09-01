namespace InventoryManagementApi.Dtos
{
    public record CreateProductDto(
        string Name,
        string Sku,
        decimal Price,
        int StockQuantity);

    public record UpdateStockDto(int Quantity);

    public record ProductResponseDto(
        int Id,
        string Name,
        string Sku,
        decimal Price,
        int StockQuantity);
}