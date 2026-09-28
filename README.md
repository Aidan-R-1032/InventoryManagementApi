# Inventory Management API

![CI](https://github.com/Aidan-R-1032/InventoryManagementApi/actions/workflows/ci.yml/badge.svg)

A RESTful inventory and order management API built with ASP.NET Core Minimal API,
Entity Framework Core, and SQLite. Features a complete JWT authentication system
with role-based authorization, refresh token rotation, password reset via email,
and comprehensive test coverage with a GitHub Actions CI pipeline.

## Tech Stack

- **ASP.NET Core Minimal API** (.NET 10)
- **Entity Framework Core** with SQLite
- **JWT Bearer Authentication** with refresh token rotation
- **BCrypt.Net** for password hashing
- **MailKit** for SMTP email delivery
- **xUnit** for unit and integration testing
- **Scalar** for interactive API documentation
- **GitHub Actions** for continuous integration

## Features

### Inventory
- Full CRUD for products with unique SKU enforcement
- Multi-item order placement with atomic stock validation
- Order cancellation with automatic stock restoration
- Price snapshotting — order items record the price at time of purchase
- Cascade delete on orders, Restrict delete on products referenced by orders

### Authentication & Security
- Registration and login with bcrypt password hashing
- JWT access tokens (15 min expiry) + refresh tokens (7 day expiry)
- Refresh token rotation — every refresh issues a new token and invalidates the old one
- Token reuse detection — presenting a replaced token revokes the entire token family
- Logout via refresh token revocation
- Role-based authorization (Admin and Staff roles)
- Rate limiting on auth endpoints (5 req/min) and API endpoints (100 req/min)
- Account lockout after 5 consecutive failed login attempts (15 min lockout)
- Vague login errors preventing user enumeration
- Password strength validation (min 8 chars, uppercase, number, special character)
- Email format validation
- Password reset via cryptographically secure single-use tokens (15 min expiry)
- Environment-based email provider — console logging in Development, SMTP in Production

## Architecture & Design Patterns

- **IProductService / IOrderService / IAuthService** — dependency-injected interfaces
- **IEmailService** — abstraction over email delivery, swappable per environment
- **DTOs + DtoMapper** — separates API contracts from domain models
- **Fluent API configuration** — explicit relationships, constraints, and indexes
- **Validate before mutate** — all stock checks complete before any stock is decremented
- **Token family revocation** — reuse of a rotated refresh token revokes all active tokens for that user

## Project Structure
```
InventoryManagement/
├── InventoryManagementApi/
│ ├── Models/ # Product, Order, OrderItem, User,
│ │ # RefreshToken, PasswordResetToken
│ ├── Dtos/ # Request/response shapes and mapper
│ ├── Data/ # EF Core DbContext with relationship configuration
│ ├── Services/ # Business logic, auth, email (console + SMTP)
│ ├── Endpoints/ # Minimal API route definitions
│ └── Program.cs # App configuration and DI registration
└── InventoryManagementApi.Tests/
├── Unit/ # Service-layer tests (27 tests)
├── Integration/ # HTTP pipeline tests (35 tests)
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

### Auth

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | /api/auth/register | Register a new user (Staff role by default) | No |
| POST | /api/auth/login | Login and receive access + refresh tokens | No |
| POST | /api/auth/refresh | Exchange a refresh token for a new token pair | No |
| POST | /api/auth/logout | Revoke a refresh token | No |
| POST | /api/auth/forgot-password | Request a password reset token | No |
| POST | /api/auth/reset-password | Reset password using a valid token | No |

### Products

| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | /api/products | Get all products ordered by name | Staff, Admin |
| GET | /api/products/{id} | Get a product by ID | Staff, Admin |
| GET | /api/products/sku/{sku} | Get a product by SKU | Staff, Admin |
| POST | /api/products | Create a new product | Admin |
| PATCH | /api/products/{id}/stock | Update stock quantity | Admin |
| DELETE | /api/products/{id} | Delete a product | Admin |

### Orders

| Method | Endpoint | Description | Role |
|--------|----------|-------------|------|
| GET | /api/orders | Get all orders | Staff, Admin |
| GET | /api/orders/{id} | Get an order by ID | Staff, Admin |
| POST | /api/orders | Place a new order | Admin |
| PATCH | /api/orders/{id}/cancel | Cancel an order and restore stock | Admin |

## Security

- Passwords are hashed with **bcrypt** — plaintext passwords are never stored or logged
- JWT tokens signed with **HMAC-SHA256**, validated on every request
- Login errors return `401` regardless of whether email or password was wrong,
  preventing user enumeration
- Refresh token rotation means a stolen token becomes useless the moment the
  legitimate user next refreshes
- Token reuse detection revokes all active sessions for a user on suspected theft
- Password reset tokens are single-use, cryptographically random, and expire after 15 minutes
- Token revocation is not implemented for access tokens — JWTs are stateless and
  valid until expiry. In production, a blocklist using Redis would handle this.
- `Strict-Transport-Security` headers should be configured at the reverse proxy
  level in production
- **Note:** The JWT secret and SMTP credentials should live in environment variables
  or a secrets manager (e.g. Azure Key Vault) in production, not in config files

## Key Design Decisions

**Restrict vs Cascade delete** — deleting a product that has been ordered returns
`409 Conflict` rather than silently removing historical order data.

**Price snapshotting** — `OrderItem.UnitPriceAtTimeOfOrder` records the price at
the moment of purchase so historical orders remain accurate if prices change.

**Validate before mutate** — `PlaceOrderAsync` checks stock for all items before
decrementing any, so a partial failure leaves nothing modified.

**Email service abstraction** — `IEmailService` is implemented by `ConsoleEmailService`
in Development (logs to console) and `SmtpEmailService` in Production (sends via Gmail
SMTP using MailKit). Swapping providers requires no changes to business logic.

**Refresh token family revocation** — if a token that has already been rotated is
presented again, the entire token family for that user is revoked. This forces
re-authentication and limits the damage window if a refresh token is stolen.

## Test Coverage

| Category | Count | Scope |
|----------|-------|-------|
| Unit | 27 | Service layer business logic and validation |
| Integration — Products | 12 | Full HTTP pipeline including auth |
| Integration — Auth | 23 | Registration, login, tokens, password reset |
| **Total** | **62** | **62/62 passing** |

## Example — Full Auth Flow

**Register:**
```json
POST /api/auth/register
{ "username": "jane", "email": "jane@example.com", "password": "Password123!" }
```

**Login:**
```json
POST /api/auth/login
{ "email": "jane@example.com", "password": "Password123!" }
→ { "accessToken": "eyJ...", "refreshToken": "abc...", "role": "Staff", ... }
```

**Use access token:**
