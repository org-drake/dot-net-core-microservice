using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace dot_net_core_microservice.Infrastructure.Persistence;

// Used only by `dotnet ef migrations add` to generate migration files at design
// time - the connection string here is never used to actually connect to a
// database, so it does not need real credentials.
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=design-time;TrustServerCertificate=True");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
