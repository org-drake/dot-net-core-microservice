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

    // Host/Port/Database live in appsettings (not secret). User/Password must never be
    // committed - supply them via the Sql:User / Sql:Password user-secrets keys locally,
    // or the Sql__User / Sql__Password environment variables (e.g. from a k8s Secret).
    private static string BuildConnectionString(IConfiguration configuration)
    {
        var sql = configuration.GetSection("Sql");

        var user = sql["User"];
        var password = sql["Password"];
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "SQL Server credentials are not configured. Set Sql:User and Sql:Password via " +
                "dotnet user-secrets (local dev) or the Sql__User / Sql__Password environment " +
                "variables (e.g. sourced from a Kubernetes Secret).");
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{sql["Host"]},{sql["Port"]}",
            InitialCatalog = sql["Database"],
            UserID = user,
            Password = password,
            Encrypt = true,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }
}
