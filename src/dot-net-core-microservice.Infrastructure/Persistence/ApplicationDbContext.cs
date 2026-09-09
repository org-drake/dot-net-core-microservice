using dot_net_core_microservice.Application.Common.Interfaces;
using dot_net_core_microservice.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_microservice.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
