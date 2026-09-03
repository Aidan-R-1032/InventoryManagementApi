using InventoryManagementApi.Dtos;
using InventoryManagementApi.Services;

namespace InventoryManagementApi.Endpoints
{
    public static class OrderEndpoints
    {
        public static void MapOrderEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/orders")
                .WithTags("Orders");

            // GET all orders
            group.MapGet("/", async (IOrderService orderService) =>
            {
                var orders = await orderService.GetAllOrdersAsync();
                return Results.Ok(orders.Select(DtoMapper.ToOrderResponse));
            })
            .WithName("GetAllOrders")
            .WithSummary("Get all orders");

            // GET order by ID
            group.MapGet("/{id:int}", async (int id, IOrderService orderService) =>
            {
                var order = await orderService.GetOrderByIdAsync(id);
                return order is null
                    ? Results.NotFound($"Order with ID {id} was not found.")
                    : Results.Ok(DtoMapper.ToOrderResponse(order));
            })
            .WithName("GetOrderById")
            .WithSummary("Get an order by ID");

            // POST place order
            group.MapPost("/", async (CreateOrderDto? dto, IOrderService orderService) =>
            {
                if (dto is null)
                    return Results.BadRequest("Request body cannot be null.");

                try
                {
                    var items = dto.Items
                        .Select(i => new OrderItemRequest(i.ProductId, i.Quantity))
                        .ToList();

                    var order = await orderService.PlaceOrderAsync(dto.CustomerName, items);
                    return Results.Created(
                        $"/api/orders/{order.Id}",
                        DtoMapper.ToOrderResponse(order));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(ex.Message);
                }
            })
            .WithName("PlaceOrder")
            .WithSummary("Place a new order");

            // PATCH cancel order
            group.MapPatch("/{id:int}/cancel", async (int id, IOrderService orderService) =>
            {
                try
                {
                    var success = await orderService.CancelOrderAsync(id);
                    return success
                        ? Results.NoContent()
                        : Results.NotFound($"Order with ID {id} was not found.");
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(ex.Message);
                }
            })
            .WithName("CancelOrder")
            .WithSummary("Cancel an order and restore stock");
        }
    }
}