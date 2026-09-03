using InventoryManagementApi.Dtos;
using InventoryManagementApi.Services;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementApi.Endpoints
{
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/products")
                .WithTags("Products");

            // GET all products
            group.MapGet("/", async (IProductService productService) =>
            {
                var products = await productService.GetAllProductsAsync();
                return Results.Ok(products.Select(DtoMapper.ToProductResponse));
            })
            .WithName("GetAllProducts")
            .WithSummary("Get all products ordered by name");

            // GET product by ID
            group.MapGet("/{id:int}", async (int id, IProductService productService) =>
            {
                var product = await productService.GetProductByIdAsync(id);
                return product is null
                    ? Results.NotFound($"Product with ID {id} was not found.")
                    : Results.Ok(DtoMapper.ToProductResponse(product));
            })
            .WithName("GetProductById")
            .WithSummary("Get a product by ID");

            // GET product by SKU
            group.MapGet("/sku/{sku}", async (string sku, IProductService productService) =>
            {
                var product = await productService.GetProductBySkuAsync(sku);
                return product is null
                    ? Results.NotFound($"Product with SKU '{sku}' was not found.")
                    : Results.Ok(DtoMapper.ToProductResponse(product));
            })
            .WithName("GetProductBySku")
            .WithSummary("Get a product by SKU");

            // POST create product
            group.MapPost("/", async (CreateProductDto? dto, IProductService productService) =>
            {
                if (dto is null)
                    return Results.BadRequest("Request body cannot be null.");

                try
                {
                    var product = await productService.CreateProductAsync(
                        dto.Name,
                        dto.Sku,
                        dto.Price,
                        dto.StockQuantity);

                    return Results.Created(
                        $"/api/products/{product.Id}",
                        DtoMapper.ToProductResponse(product));
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
            .WithName("CreateProduct")
            .WithSummary("Create a new product");

            // PATCH update stock
            group.MapPatch("/{id:int}/stock", async (int id, UpdateStockDto? dto, IProductService productService) =>
            {
                if (dto is null)
                    return Results.BadRequest("Request body cannot be null.");

                try
                {
                    var product = await productService.UpdateStockAsync(id, dto.Quantity);
                    return product is null
                        ? Results.NotFound($"Product with ID {id} was not found.")
                        : Results.Ok(DtoMapper.ToProductResponse(product));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
            .WithName("UpdateStock")
            .WithSummary("Update stock quantity for a product");

            // DELETE product
            group.MapDelete("/{id:int}", async (int id, IProductService productService) =>
            {
                try
                {
                    var success = await productService.DeleteProductAsync(id);
                    return success
                        ? Results.NoContent()
                        : Results.NotFound($"Product with ID {id} was not found.");
                }
                catch (DbUpdateException)
                {
                    return Results.Conflict(
                        "Cannot delete this product because it is referenced by existing orders.");
                }
            })
            .WithName("DeleteProduct")
            .WithSummary("Delete a product");
        }
    }
}