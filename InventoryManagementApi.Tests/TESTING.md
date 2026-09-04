# Testing Strategy

## Why integration tests were harder than expected

Setting up `WebApplicationFactory` with a swapped database provider turned out
to be non-trivial in EF Core 10. Here's what went wrong and why the final
approach works.

### Attempt 1 — EF Core InMemory provider

The first instinct was to swap the real SQLite database for EF Core's built-in
InMemory provider (`UseInMemoryDatabase`). This seemed clean but had two problems:

**Problem A — Dual provider conflict**
When `ConfigureServices` adds InMemory on top of the existing SQLite registration,
EF Core throws because two database providers are registered at the same time. Removing
the right descriptors from the DI container is tricky because EF Core 10 registers
many internal services under its own namespaces, and removing only `DbContextOptions`
left orphaned provider services behind.

**Problem B — InMemory doesn't enforce constraints**
Even when the provider swap succeeded, the InMemory provider doesn't enforce
database-level constraints like how the `Sku` must have a unique index. This meant a duplicate
SKU insert would succeed in tests but fail in production, essentially giving a **false positive**.

### Attempt 2 — Shared `IClassFixture` factory

xUnit's `IClassFixture<WebApplicationFactory<Program>>` shares one factory instance
across all tests in a class for performance. The problem is that the shared factory
also shares **one in-memory database**, so data created in test A was visible to test B.
Creating `WGT-001` in one test caused a 409 Conflict in the next test that also
tried to create `WGT-001`.

Moving `WithWebHostBuilder` into `CreateClient()` didn't help because
`WebApplicationFactory` caches its host — calling `WithWebHostBuilder` on an already-
built factory just returns the cached instance rather than building a new one.

### What actually works — SQLite in-memory with a shared connection

The final approach uses a custom `TestWebApplicationFactory` subclass that:

1. Opens a `SqliteConnection` with `DataSource=:memory:` and keeps it open for
   the factory's lifetime
2. Registers the DbContext to use that specific connection via `UseSqlite(_connection)`
3. Calls `db.Database.EnsureCreated()` to build the schema from the model
4. Closes the connection when the factory is disposed

Each test instantiates its own `TestWebApplicationFactory` — so each test gets its
own connection, its own in-memory database, and a freshly created schema. Tests are
fully isolated with zero shared state.

**Why keep the connection open?**
SQLite's in-memory databases are tied to a connection. The moment the connection
closes, the database is destroyed. By keeping the connection alive for the factory's
lifetime, the database persists across the multiple requests a single test makes
(e.g. POST to create, then GET to verify).

**Why not use `EnsureDeleted()` and reset between tests?**
Resetting a shared database between tests requires careful teardown and is fragile
under parallel test execution. Giving each test its own connection is simpler,
faster, and eliminates the entire category of test pollution bugs.

**Why not use a real SQLite file?**
A file-based SQLite database would persists between test runs, requiring cleanup logic.
It also creates file I/O contention when tests run in parallel. In-memory is
faster and self-cleaning.

## Test structure 

```
InventoryManagementApi.Tests/
├── Unit/
│ ├── OrderServiceTests.cs (11 tests — business logic, stock validation)
│ └── ProductServiceTests.cs (17 tests — CRUD, validation, SKU uniqueness)
└── Integration/
└── ProductEndpointsTests.cs (9 tests — full HTTP pipeline via WebApplicationFactory)
```

## Unit vs Integration

**Unit tests** (`Unit/`) test service classes in isolation using EF Core's InMemory
provider directly — no HTTP layer, no routing, no middleware. They are fast and
focused on business rules: can you order more than available stock? Does cancelling
an order restore stock? Does a zero-quantity order throw?

**Integration tests** (`Integration/`) test the full request pipeline using
`WebApplicationFactory` — real HTTP requests go through routing, middleware,
endpoint handlers, DI, and the database. They verify that the pieces work together:
does a POST to `/api/products` return 201 with the right shape? Does a duplicate
SKU return 409?

The InMemory provider is acceptable for unit tests because they test logic, not
constraints. The SQLite in-memory connection is required for integration tests
because they need real constraint enforcement to be meaningful.

## Running the tests

```bash
dotnet test InventoryManagement.slnx
```

Current result: **36/36 passing** (27 unit, 9 integration)