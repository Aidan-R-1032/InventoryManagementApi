using Microsoft.EntityFrameworkCore;
using InventoryManagementApi.Data;
using InventoryManagementApi.Models;

namespace InventoryManagementApi.Services
{
    public class OrderService : IOrderService
    {
        private readonly InventoryDbContext _context;

        public OrderService(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order> PlaceOrderAsync(string customerName, List<OrderItemRequest> items)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                throw new ArgumentException("Customer name cannot be empty.", nameof(customerName));

            if (items is null || items.Count == 0)
                throw new ArgumentException("Order must contain at least one item.", nameof(items));

            if (items.Any(i => i.Quantity <= 0))
                throw new ArgumentException("All item quantities must be greater than zero.", nameof(items));

            // Load all products involved in this order in one query
            var productIds = items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            // Validate every product exists
            var missingIds = productIds.Except(products.Select(p => p.Id)).ToList();
            if (missingIds.Count > 0)
            {
                throw new InvalidOperationException($"Products not found: {string.Join(", ", missingIds)}");
            }
                
            // Validate stock for every item before touching anything
            var stockErrors = new List<string>();
            foreach (var item in items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                if (item.Quantity > product.StockQuantity)
                {
                    stockErrors.Add($"'{product.Name}' has {product.StockQuantity} in stock but {item.Quantity} were requested.");
                }
            }

            if (stockErrors.Count > 0)
            {
                throw new InvalidOperationException($"Insufficient stock:\n{string.Join("\n", stockErrors)}");
            }

            // All checks passed — build the order
            var order = new Order
            {
                CustomerName = customerName.Trim(),
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Confirmed
            };

            foreach (var item in items)
            {
                var product = products.First(p => p.Id == item.ProductId);

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPriceAtOrderTime = product.Price
                });

                // Decrement stock
                product.StockQuantity -= item.Quantity;
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<bool> CancelOrderAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null) return false;

            if (order.Status == OrderStatus.Cancelled)
                throw new InvalidOperationException("Order is already cancelled.");

            // Restore stock for each item
            foreach (var item in order.OrderItems)
            {
                item.Product.StockQuantity += item.Quantity;
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}