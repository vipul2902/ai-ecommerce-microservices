# User Service

**Status:** Implemented (Phase 2). Owns user identity: registration, login, and profile data.

See [docs/architecture.md](../../docs/architecture.md) for how this service fits into the
overall system, and [docs/development-roadmap.md](../../docs/development-roadmap.md) for
what comes next.

## Responsibility

- User registration and login (JWT-based authentication).
- User profile storage and retrieval.
- Owns its own SQL Server database (`UserServiceDb`) — no other service accesses it directly.

## Project Layout

A pragmatic four-project layered structure — not a full "Clean Architecture" template,
just enough separation to keep concerns distinct for a learning project:

```
UserService.sln
├── UserService.Api/              ASP.NET Core Web API: controllers, Program.cs, config, DI wiring
├── UserService.Application/      Business logic: UserAccountService, DTOs, validation, interfaces
├── UserService.Domain/           Domain model: the User entity, no framework dependencies
└── UserService.Infrastructure/   EF Core DbContext + migrations, password hashing, JWT issuing
```

Dependency direction: `Api → Application, Infrastructure` · `Infrastructure → Application, Domain`
· `Application → Domain`. Domain depends on nothing else.

## API Endpoints

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/users/register` | none | Register a new user. Returns the created profile (no password fields). |
| POST | `/api/users/login` | none | Authenticate with email + password. Returns a JWT + profile. |
| GET | `/api/users/me` | JWT | Returns the authenticated user's own profile. |
| PUT | `/api/users/me` | JWT | Updates the authenticated user's first/last name only. |

Swagger UI is available at `/swagger` when running in the Development environment, with a
"Bearer" auth button pre-wired for pasting a JWT from `/login`.

## Authentication Flow

1. `POST /api/users/register` — email + password validated, checked for uniqueness, password
   hashed (never stored in plaintext), user persisted with role `Customer`.
2. `POST /api/users/login` — email looked up, password verified against the stored hash
   (constant-time comparison via ASP.NET Core Identity's `PasswordHasher<T>`). On success, a
   JWT is issued containing the user's id, email, name, and role as claims.
3. The client sends that JWT as `Authorization: Bearer <token>` on subsequent requests.
4. `GET /api/users/me` / `PUT /api/users/me` — the JWT bearer middleware validates the token's
   signature, issuer, audience, and expiry; the controller reads the user id from the token's
   claims (never from a client-supplied id) to know whose profile to act on.

## Database

SQL Server via EF Core, one table (`Users`): `Id`, `FirstName`, `LastName`, `Email`
(unique index), `PasswordHash`, `Role`, `CreatedAt`, `UpdatedAt`. See
[UserService.Infrastructure/Persistence](UserService.Infrastructure/Persistence) for the
`DbContext` and the generated migration.

Per the [database-per-service principle](../../docs/architecture.md#5-database-per-service),
this database is private to this service — other services must never connect to it directly.

## Configuration

No secrets are hardcoded. Required configuration:

| Key | Purpose |
|---|---|
| `ConnectionStrings:UserServiceDb` | SQL Server connection string |
| `Jwt:Issuer` / `Jwt:Audience` | JWT validation values |
| `Jwt:Key` | HMAC-SHA256 signing key, **at least 32 characters** |
| `Jwt:ExpiryMinutes` | Token lifetime (default 60) |

`appsettings.json` only holds non-secret defaults for `Jwt:Issuer`/`Jwt:Audience`/
`Jwt:ExpiryMinutes`; `ConnectionStrings:UserServiceDb` and `Jwt:Key` are left empty there
and must be supplied via user-secrets (local dev) or environment variables (anywhere else).
Missing or too-short values fail fast at startup with a clear error, rather than silently
running insecurely.

## Running Locally (no Docker required)

Requires the .NET 8 SDK and a reachable SQL Server instance (a local install, a remote dev
server, or any SQL Server you already have — setting one up is outside this phase's scope,
see [docs/development-roadmap.md](../../docs/development-roadmap.md) Phase 10 for the future
Dockerized local environment).

```bash
cd src/UserService

# One-time: point the app at your database and a signing key, without touching the repo.
dotnet user-secrets set "ConnectionStrings:UserServiceDb" "Server=localhost;Database=UserServiceDb;Trusted_Connection=True;TrustServerCertificate=True;" --project UserService.Api
dotnet user-secrets set "Jwt:Key" "a-random-development-only-secret-at-least-32-chars" --project UserService.Api

# Create the database schema (uses the connection string above).
dotnet tool restore
dotnet tool run dotnet-ef database update --project UserService.Infrastructure --startup-project UserService.Api

# Run the API.
dotnet run --project UserService.Api
```

Then open `http://localhost:<port>/swagger` (the port is printed on startup) to try the
endpoints. Alternatively, set the same two values via environment variables instead of
user-secrets: `ConnectionStrings__UserServiceDb` and `Jwt__Key`.

## Tests

Unit tests: [tests/UserService.UnitTests](../../tests/UserService.UnitTests) — business logic
in `UserAccountService` (registration, duplicate email, login, password verification) and the
real password hasher, with the database and JWT generator mocked/isolated so no external
dependency is needed.

Integration tests: [tests/UserService.IntegrationTests](../../tests/UserService.IntegrationTests)
— boot the real API host through `WebApplicationFactory`, with the EF Core InMemory provider
substituted for SQL Server, exercising the full HTTP pipeline (validation, auth, controllers)
for register/login/me/update-me, including the unauthorized-access case.

```bash
cd src/UserService
dotnet test UserService.sln
```
