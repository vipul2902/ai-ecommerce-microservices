# Product Service

**Status:** Implemented (Phase 3). Owns the product catalog: products, categories, pricing,
and stock visibility.

See [docs/architecture.md](../../docs/architecture.md) for how this service fits into the
overall system, and [docs/development-roadmap.md](../../docs/development-roadmap.md) for
what comes next.

## Responsibility

- Product catalog CRUD: create, read, update, delete, and list-with-category-filter.
- Owns its own SQL Server database (`ProductServiceDb`) — no other service accesses it directly.

## No Authentication Yet — Intentionally

Every endpoint here is currently public. This isn't an oversight: how a service verifies a
caller's identity across service boundaries is a **Phase 5 (service-to-service communication)
/ Phase 6 (API Gateway)** design question that hasn't been made yet (see
[docs/architecture.md](../../docs/architecture.md), sections 4 and 8). Wiring JWT validation
into this service now — before that cross-service design exists — would mean guessing at it
twice. This will change once those phases are reached.

## Project Layout

Same four-project layered structure as [UserService](../UserService), for consistency across
services — not a shared library (each service's `Application`/`Infrastructure` are its own
independent code, deliberately duplicated where needed rather than referenced, so services stay
independently deployable):

```
ProductService.sln
├── ProductService.Api/              ASP.NET Core Web API: controllers, Program.cs, config
├── ProductService.Application/      ProductCatalogService, DTOs, validation, interfaces
├── ProductService.Domain/           The Product entity, no framework dependencies
└── ProductService.Infrastructure/   EF Core DbContext + migrations, repository
```

## API Endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/api/products` | List products. Optional `?category=` exact-match filter. |
| GET | `/api/products/{id}` | Get a single product. `404` if not found. |
| POST | `/api/products` | Create a product. `409` if the SKU already exists. |
| PUT | `/api/products/{id}` | Update name/description/category/price/stock. SKU is immutable. |
| DELETE | `/api/products/{id}` | Delete a product. `204` on success, `404` if not found. |

Swagger UI is available at `/swagger` when running in the Development environment.

## Database

SQL Server via EF Core, one table (`Products`): `Id`, `Name`, `Description`, `Sku` (unique
index), `Category`, `Price` (`decimal(18,2)`), `StockQuantity`, `CreatedAt`, `UpdatedAt`. See
[ProductService.Infrastructure/Persistence](ProductService.Infrastructure/Persistence) for the
`DbContext` and the generated migration.

Per the [database-per-service principle](../../docs/architecture.md#5-database-per-service),
this database is private to this service.

## Configuration

No secrets are hardcoded. `ConnectionStrings:ProductServiceDb` is left empty in
`appsettings.json` and must be supplied via user-secrets (local dev) or the
`ConnectionStrings__ProductServiceDb` environment variable. A missing value fails fast with a
clear error the first time the database is touched, rather than running silently misconfigured.

## Running Locally (no Docker required)

Requires the .NET 8 SDK and a reachable SQL Server instance (see
[UserService's README](../UserService/README.md#running-locally-no-docker-required) for options
— the same "no Docker yet" caveat applies here).

```bash
cd src/ProductService

dotnet user-secrets set "ConnectionStrings:ProductServiceDb" "Server=localhost;Database=ProductServiceDb;Trusted_Connection=True;TrustServerCertificate=True;" --project ProductService.Api

dotnet tool restore
dotnet tool run dotnet-ef database update --project ProductService.Infrastructure --startup-project ProductService.Api

dotnet run --project ProductService.Api
```

Then open `http://localhost:<port>/swagger` (the port is printed on startup).

## Tests

Unit tests: [tests/ProductService.UnitTests](../../tests/ProductService.UnitTests) — business
logic in `ProductCatalogService` (create, duplicate SKU, get/update/delete not-found, category
filtering, SKU immutability on update) with the repository mocked.

Integration tests: [tests/ProductService.IntegrationTests](../../tests/ProductService.IntegrationTests)
— boot the real API host through `WebApplicationFactory`, with the EF Core InMemory provider
substituted for SQL Server, exercising the full CRUD HTTP pipeline including validation and
category filtering.

```bash
cd src/ProductService
dotnet test ProductService.sln
```
