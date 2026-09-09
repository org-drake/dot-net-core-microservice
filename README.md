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

This is `Sql:AuthenticationMode = "SqlPassword"` (the default). For AKS deployments against Azure SQL Database, prefer `"ManagedIdentity"` instead — see below.

## Passwordless Azure SQL access via Managed Identity (AKS)
`Sql:AuthenticationMode` can be set to `"ManagedIdentity"` so the app authenticates to **Azure SQL Database** with Microsoft Entra Workload ID instead of a stored SQL login — no `Sql:User`/`Sql:Password` at all. This is Microsoft's recommended auth model for Azure-hosted workloads, and is what `k8s/all.yaml` deploys by default.

It only works against Azure SQL Database or Managed Instance (not the self-hosted dev SQL Server at `192.168.29.56`, which has no Microsoft Entra integration) and requires one-time setup:
1. Run `scripts/setup-workload-identity.sh` — it enables Workload ID/OIDC issuer on the AKS cluster, creates a user-assigned managed identity, and federates it with the `dot-net-core-microservice` ServiceAccount in `k8s/all.yaml`.
2. Paste the identity's client ID into that ServiceAccount's `azure.workload.identity/client-id` annotation in `k8s/all.yaml`.
3. As the SQL server's Microsoft Entra admin, run the `CREATE USER ... FROM EXTERNAL PROVIDER` script the setup script prints, to grant the identity `db_datareader`/`db_datawriter` on the database.
4. Set `Sql__Host` in `k8s/all.yaml` to `<your-server>.database.windows.net`.

Locally, `dotnet ef database update` still needs `SqlPassword` credentials (or a `sqlcmd`/portal connection using your own Entra login) — Workload ID only applies inside the AKS pod.

## Configure API credentials
The `/api/products` endpoints always require HTTP Basic Authentication. If Bearer auth is also configured (see below), **either** scheme authorizes the request — whichever one succeeds.

**Basic Auth.** Set `BasicAuth:Username` and `BasicAuth:Password` the same way as the SQL credentials above:

```bash
dotnet user-secrets set "BasicAuth:Username" "<api-username>"
dotnet user-secrets set "BasicAuth:Password" "<api-password>"
```

or via environment variables (`BasicAuth__Username` / `BasicAuth__Password`, e.g. from a k8s Secret). The app throws a clear startup error if these are missing.

**Bearer (Microsoft Entra ID) — opt-in.** Unset (the local dev default), the API only accepts Basic Auth; nothing else changes. To enable it, run `scripts/register-entra-api.sh` once to register this API as an App Registration, then set **both** `AzureAd:TenantId` and `AzureAd:Audience` (the `api://<app-id>` value the script prints):

```bash
dotnet user-secrets set "AzureAd:TenantId" "<your-tenant-id>"
dotnet user-secrets set "AzureAd:Audience" "api://<your-api-app-id>"
```

or via environment variables (`AzureAd__TenantId` / `AzureAd__Audience`). These aren't secrets — Entra validates the token signature against its own public JWKS endpoint, no key is stored here — but the app throws a clear startup error if only one of the two is set (both-or-neither). Once enabled, a caller (e.g. another service's managed identity granted the app role, see the script's output) sends `Authorization: Bearer <token>` instead of `Authorization: Basic ...`.

`/healthz` is not protected by either scheme, so container/k8s probes keep working without credentials.

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
# Basic Auth
curl -u <api-username>:<api-password> http://localhost:5191/api/products
curl -u <api-username>:<api-password> -X POST http://localhost:5191/api/products \
  -H "Content-Type: application/json" \
  -d '{"sku":"SKU-1","name":"Widget","price":9.99,"stockQuantity":10}'

# Bearer (Microsoft Entra ID)
TOKEN=$(az account get-access-token --resource api://<your-api-app-id> --query accessToken -o tsv)
curl -H "Authorization: Bearer $TOKEN" http://localhost:5191/api/products
```

## Notes
- HTTPS profile uses the local development certificate that ships with the .NET SDK; trust it if your browser warns.
- Swagger is enabled for the Development environment by default. Use the "Authorize" button to supply Basic (and Bearer, if enabled) credentials for try-it-out requests.
- All `/api/products` endpoints require Basic Auth, or a Bearer token if that's been enabled; see "Configure API credentials" above.
- `/healthz` includes an EF Core DB connectivity check (`AddDbContextCheck<ApplicationDbContext>()`) — it reports `503 Unhealthy` if SQL Server is unreachable rather than the app crashing.
- CORS is deny-by-default. To allow a browser client, add its origin(s) to `Cors:AllowedOrigins` in `appsettings.json` (or `appsettings.Development.json`), e.g. `["https://localhost:3000"]`.
- Unhandled exceptions are converted to a generic ProblemDetails (RFC 7807) response by the built-in `UseExceptionHandler()` middleware instead of leaking a raw stack trace.
- See `scripts/setup-github-oidc.sh` for the Azure AD OIDC setup the CI/CD pipeline needs.
- See `scripts/setup-workload-identity.sh` for the Microsoft Entra Workload ID setup behind `Sql:AuthenticationMode = "ManagedIdentity"`.
- See `scripts/register-entra-api.sh` for the App Registration setup behind Bearer auth.
