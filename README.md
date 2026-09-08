# Inventory Management API


![CI](https://github.com/Aidan-R-1032/InventoryManagementApi/actions/workflows/ci.yml/badge.svg)


A RESTful inventory and order management API built with ASP.NET Core Minimal API,

Entity Framework Core, and SQLite. Designed to demonstrate backend development

practices including relational data modeling, business rule validation, unit testing,

integration testing, and automated CI with GitHub Actions.


## Tech Stack


- **ASP.NET Core Minimal API** (.NET 10)

- **Entity Framework Core** with SQLite

- **xUnit** for unit and integration testing

- **Scalar** for interactive API documentation

- **GitHub Actions** for continuous integration


## Features


- Full CRUD for products with unique SKU enforcement

- Multi-item order placement with atomic stock validation

- Order cancellation with automatic stock restoration

- Price snapshotting — order items record the price at time of purchase

- Cascade delete on orders, Restrict delete on products referenced by orders

- REST endpoints with consistent error responses (400, 404, 409)

- 36 automated tests running on every push via GitHub Actions


## Architecture & Design Patterns


- **IProductService / IOrderService** — dependency-injected service interfaces (Dependency Inversion)

- **DTOs + DtoMapper** — separates API contracts from domain models (Single Responsibility)

- **Fluent API configuration** — explicit relationship, constraint, and index setup in `InventoryDbContext`

- **Validate before mutate** — all stock checks complete before any stock is decremented


## Project Structure
``` 
InventoryManagement/ 
├── InventoryManagementApi/ 
│ ├── Models/ # Product, Order, OrderItem entities 
│ ├── Dtos/ # Request/response shapes, mapper, documentation 
│ ├── Data/ # EF Core DbContext with relationship configuration 
│ ├── Services/ # IProductService, IOrderService, implementations 
│ ├── Endpoints/ # Minimal API route definitions 
│ └── Program.cs # App configuration and DI registration 
└── InventoryManagementApi.Tests/ 
  ├── Unit/ # Service-layer tests using EF Core InMemory 
  ├── Integration/ # HTTP pipeline tests using WebApplicationFactory
  └── TESTING.md # Testing strategy and design decisions
```



## Getting Started


### Prerequisites


- [.NET 10 SDK](https://dotnet.microsoft.com/download)

- [Git](https://git-scm.com/)


### Run Locally


```bash

git clone https://github.com/Aidan-R-1032/InventoryManagementApi.git

cd InventoryManagementApi

dotnet restore InventoryManagement.slnx

dotnet run --project InventoryManagementApi/InventoryManagementApi.csproj

```


The SQLite database is created and migrations applied automatically on startup.


### API Documentation
```
http://localhost:{port}/scalar/v1
```


### Run Tests


```bash

dotnet test InventoryManagement.slnx

```


## API Endpoints


### Products


| Method | Endpoint | Description |

|--------|----------|-------------|

| GET | /api/products | Get all products ordered by name |

| GET | /api/products/{id} | Get a product by ID |

| GET | /api/products/sku/{sku} | Get a product by SKU |

| POST | /api/products | Create a new product |

| PATCH | /api/products/{id}/stock | Update stock quantity |

| DELETE | /api/products/{id} | Delete a product |


### Orders


| Method | Endpoint | Description |

|--------|----------|-------------|

| GET | /api/orders | Get all orders |

| GET | /api/orders/{id} | Get an order by ID |

| POST | /api/orders | Place a new order |

| PATCH | /api/orders/{id}/cancel | Cancel an order and restore stock |


## Key Design Decisions


**Restrict vs Cascade delete** — deleting a product that has been ordered returns

`409 Conflict` rather than silently removing historical order data. Orders and their

line items cascade delete together since orphaned line items are meaningless.


**Price snapshotting** — `OrderItem.UnitPriceAtOrderTime` records the price at

the moment of purchase. If a product's price changes later, historical orders remain

accurate.


**Validate before mutate** — `PlaceOrderAsync` checks stock for all items in an

order before decrementing any of them. If item 3 of 5 fails the stock check,

nothing has been modified.


## Example Request


Place an order:


```json

POST /api/orders

{

  "customerName": "Jane Smith",

  "items": [

    { "productId": 1, "quantity": 2 },

    { "productId": 3, "quantity": 1 }

  ]

}

```


Example response:


```json

{

  "id": 1,

  "customerName": "Jane Smith",

  "orderDate": "2026-09-06T00:00:00Z",

  "status": "Confirmed",

  "items": [

    {

      "productId": 1,

      "productName": "Widget",

      "quantity": 2,

      "unitPriceAtTimeOfOrder": 9.99

    },

    {

      "productId": 3,

      "productName": "Gadget",

      "quantity": 1,

      "unitPriceAtTimeOfOrder": 24.99

    }

  ],

  "totalAmount": 44.97

}

```


## Test Coverage


| Category | Count | Scope |

|----------|-------|-------|

| Unit | 27 | Service layer business logic |

| Integration | 9 | Full HTTP pipeline via WebApplicationFactory |

| **Total** | **36** | **36/36 passing** |
