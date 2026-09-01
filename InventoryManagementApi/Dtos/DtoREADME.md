## Data Transfer Objects (DTOs)

### ProductDtos
Classes used to transfer product data between client and server:
- `CreateProductDto` — incoming request shape for creating a product
- `UpdateStockDto` — incoming request shape for updating stock quantity
- `ProductResponseDto` — outgoing response shape for product data

### OrderDtos
Classes used to transfer order data between client and server:
- `OrderItemRequestDto` — represents a single line item in a create order request
- `CreateOrderDto` — incoming request shape for placing an order
- `OrderItemResponseDto` — outgoing response shape for a single order line item
- `OrderResponseDto` — outgoing response shape for a full order including computed total

## Why Records instead of Classes

C# `record` types are preferred for DTOs because they are:
- **Immutable** — properties cannot be changed after construction
- **Value equality** — two records with the same data are considered equal

## DtoMapper

Static class that converts between domain models and DTOs.

### `ToProductResponse`
Accepts a `Product` and returns a `ProductResponseDto`.

### `ToOrderResponse`
Accepts an `Order` and returns an `OrderResponseDto`:
- Computes `TotalAmount` from `Quantity * UnitPriceAtTimeOfOrder` per line item
- Uses null-conditional operator (`?.`) on `Product.Name` to handle cases where the navigation property wasn't eagerly loaded, falling back to `"Unknown"`