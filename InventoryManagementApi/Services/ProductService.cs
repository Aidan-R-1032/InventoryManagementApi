using InventoryManagementApi.Data;
using InventoryManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementApi.Services
{
    public class ProductService : IProductService
    {
        private readonly InventoryDbContext _context;

        public ProductService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product?> GetProductBySkuAsync(string sku)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Sku == sku);
        }

        public async Task<Product> CreateProductAsync(string name, string sku, decimal price, int stockQuantity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Product name cannot be empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU cannot be empty.", nameof(sku));

            if (price < 0)
                throw new ArgumentException("Price cannot be negative.", nameof(price));

            if (stockQuantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative.", nameof(stockQuantity));

            var existing = await GetProductBySkuAsync(sku);
            if (existing is not null)
                throw new InvalidOperationException($"A product with SKU '{sku}' already exists.");

            var product = new Product
            {
                Name = name.Trim(),
                Sku = sku.Trim().ToUpper(),
                Price = price,
                StockQuantity = stockQuantity
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> UpdateStockAsync(int id, int quantity)
        {
            if (quantity < 0)
                throw new ArgumentException("Stock quantity cannot be negative.", nameof(quantity));

            var product = await _context.Products.FindAsync(id);
            if (product is null) return null;

            product.StockQuantity = quantity;
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product is null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}