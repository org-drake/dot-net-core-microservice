using dot_net_core_microservice.Application.Common.Interfaces;
using dot_net_core_microservice.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace dot_net_core_microservice.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = BuildConnectionString(configuration);

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    // Host/Port/Database/AuthenticationMode live in appsettings (not secret).
    //
    // Sql:AuthenticationMode selects how the app authenticates:
    //   - "SqlPassword" (default): a SQL login. User/Password must never be committed -
    //     supply them via the Sql:User / Sql:Password user-secrets keys locally, or the
    //     Sql__User / Sql__Password environment variables (e.g. from a k8s Secret).
    //   - "ManagedIdentity": passwordless Microsoft Entra auth for Azure SQL Database via
    //     Microsoft Entra Workload ID. No credential is ever configured or stored - see
    //     scripts/setup-workload-identity.sh and the README for the AKS/Azure SQL setup
    //     this requires.
    private static string BuildConnectionString(IConfiguration configuration)
    {
        var sql = configuration.GetSection("Sql");
        var authenticationMode = sql["AuthenticationMode"] ?? "SqlPassword";

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{sql["Host"]},{sql["Port"]}",
            InitialCatalog = sql["Database"],
            Encrypt = true
        };

        switch (authenticationMode)
        {
            case "ManagedIdentity":
                // AKS injects AZURE_CLIENT_ID / AZURE_TENANT_ID / AZURE_FEDERATED_TOKEN_FILE
                // into the pod (the workload identity webhook, via the ServiceAccount set up
                // by scripts/setup-workload-identity.sh); Microsoft.Data.SqlClient uses them
                // to fetch an Azure SQL access token itself.
                builder.Authentication = SqlAuthenticationMethod.ActiveDirectoryWorkloadIdentity;
                builder.TrustServerCertificate = false;

                var managedIdentityClientId = sql["ManagedIdentityClientId"];
                if (!string.IsNullOrEmpty(managedIdentityClientId))
                {
                    // Only needed if the pod could authenticate as more than one identity;
                    // otherwise the client ID from AZURE_CLIENT_ID is used automatically.
                    builder.UserID = managedIdentityClientId;
                }
                break;

            case "SqlPassword":
                var user = sql["User"];
                var password = sql["Password"];
                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
                {
                    throw new InvalidOperationException(
                        "SQL Server credentials are not configured. Set Sql:User and Sql:Password via " +
                        "dotnet user-secrets (local dev) or the Sql__User / Sql__Password environment " +
                        "variables (e.g. sourced from a Kubernetes Secret).");
                }

                builder.UserID = user;
                builder.Password = password;
                builder.TrustServerCertificate = true;
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown Sql:AuthenticationMode '{authenticationMode}'. Expected 'SqlPassword' or 'ManagedIdentity'.");
        }

        return builder.ConnectionString;
    }
}
