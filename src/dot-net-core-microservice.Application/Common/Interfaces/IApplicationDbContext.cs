using dot_net_core_microservice.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_microservice.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
