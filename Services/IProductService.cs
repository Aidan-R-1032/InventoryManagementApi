using InventoryApi.Models;
using InventoryManagementApi.Models;

namespace InventoryManagementApi.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductBySkuAsync(string sku);
        Task<Product> CreateProductAsync(string name, string sku, decimal price, int stockQuantity);
        Task<Product?> UpdateStockAsync(int id, int quantity);
        Task<bool> DeleteProductAsync(int id);
    }
}