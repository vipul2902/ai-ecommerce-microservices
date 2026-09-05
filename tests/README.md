# Tests

Test projects mirror the services under [src/](../src/), added alongside each service as it
is implemented (not speculatively ahead of time).

## UserService.UnitTests

Tests `UserService.Application`'s business logic (`UserAccountService`: registration,
duplicate email, login, password verification, profile updates) with the repository, password
hasher, and JWT generator mocked, plus a focused test of the real password hasher
(`UserService.Infrastructure.Auth.PasswordHasherAdapter`). No database or network access
required.

## UserService.IntegrationTests

Boots the real `UserService.Api` host via `WebApplicationFactory<Program>`, with the EF Core
InMemory provider substituted for SQL Server (see `CustomWebApplicationFactory`), and drives
the actual HTTP endpoints — register, login, get/update profile, and unauthorized access —
end to end.

## ProductService.UnitTests

Tests `ProductService.Application`'s business logic (`ProductCatalogService`: create, duplicate
SKU, get/update/delete not-found, category filtering, SKU immutability on update) with the
repository mocked. No database or network access required.

## ProductService.IntegrationTests

Boots the real `ProductService.Api` host via `WebApplicationFactory<Program>`, with the EF Core
InMemory provider substituted for SQL Server, and drives the actual HTTP endpoints — create,
read, list with category filter, update, delete — end to end.

## Running

```bash
cd src/UserService
dotnet test UserService.sln

cd ../ProductService
dotnet test ProductService.sln
```
