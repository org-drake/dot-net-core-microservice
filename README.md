# dot-net-core-microservice

.NET 8 Web API following Clean Architecture, using MediatR for CQRS and EF Core against SQL Server.

## Project Structure
```
dot-net-core-microservice/
├── src/
│   ├── dot-net-core-microservice.Domain/          # Entities. No dependencies on other layers.
│   ├── dot-net-core-microservice.Application/     # MediatR commands/queries/handlers, DTOs,
│   │                                               # IApplicationDbContext (persistence port)
│   ├── dot-net-core-microservice.Infrastructure/   # EF Core DbContext, entity configuration,
│   │                                               # migrations, SQL Server wiring
│   └── dot-net-core-microservice/                 # ASP.NET Core Web API: controllers, Program.cs
└── tests/
    └── dot-net-core-microservice.Tests/
        ├── Controllers/                           # Controller tests (mocked ISender)
        └── Application/Products/                  # Handler tests (EF Core InMemory provider)
```

Dependencies flow inward: `Api` → `Application` + `Infrastructure` → `Application` → `Domain`. Controllers depend only on `MediatR.ISender`; they never see EF Core or the database directly.

## Prerequisites
- .NET 8 SDK installed (`dotnet --version` to verify)
- A reachable SQL Server instance (this project targets one at `192.168.29.56:1433`, e.g. running as a Docker container)

## Configure SQL Server credentials
`Sql:Host`, `Sql:Port`, and `Sql:Database` are set in `appsettings.json` (non-secret). **Credentials are never committed** — supply them via one of:

**Local development (user-secrets):**
```bash
cd src/dot-net-core-microservice
dotnet user-secrets set "Sql:User" "<your-sql-username>"
dotnet user-secrets set "Sql:Password" "<your-sql-password>"
```

**Environment variables** (works anywhere, including containers):
```bash
export Sql__User="<your-sql-username>"
export Sql__Password="<your-sql-password>"
```

The app throws a clear startup error if these are missing rather than silently failing later.

## Apply the database schema
Migrations are generated but **not** auto-applied — run this yourself against the real server once credentials are set (locally, via user-secrets or env vars as above):
```bash
dotnet ef database update \
  --project src/dot-net-core-microservice.Infrastructure \
  --startup-project src/dot-net-core-microservice
```
This creates the `Products` table on the target database.

## Restore & run
```bash
# From the root directory
dotnet restore dot-net-core-microservice.sln
dotnet run --project src/dot-net-core-microservice/
```
The app launches with Swagger UI at:
- http://localhost:5191/swagger
- https://localhost:7291/swagger

## Run tests
```bash
# From the root directory
dotnet test dot-net-core-microservice.sln
```
Handler tests use the EF Core InMemory provider and don't need a real SQL Server connection.

## Sample requests
```bash
curl http://localhost:5191/api/products
curl -X POST http://localhost:5191/api/products \
  -H "Content-Type: application/json" \
  -d '{"sku":"SKU-1","name":"Widget","price":9.99,"stockQuantity":10}'
```

## Notes
- HTTPS profile uses the local development certificate that ships with the .NET SDK; trust it if your browser warns.
- Swagger is enabled for the Development environment by default.
- `/healthz` includes an EF Core DB connectivity check (`AddDbContextCheck<ApplicationDbContext>()`) — it reports `503 Unhealthy` if SQL Server is unreachable rather than the app crashing.
- CORS is deny-by-default. To allow a browser client, add its origin(s) to `Cors:AllowedOrigins` in `appsettings.json` (or `appsettings.Development.json`), e.g. `["https://localhost:3000"]`.
- Unhandled exceptions are converted to a generic ProblemDetails (RFC 7807) response by the built-in `UseExceptionHandler()` middleware instead of leaking a raw stack trace.
- See `scripts/setup-github-oidc.sh` for the Azure AD OIDC setup the CI/CD pipeline needs.
