using dot_net_core_microservice.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_microservice.Application.Products.Queries.GetAllProducts;

public class GetAllProductsQueryHandler(IApplicationDbContext context) : IRequestHandler<GetAllProductsQuery, List<ProductDto>>
{
    public Task<List<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        return context.Products
            .OrderBy(p => p.Id)
            .Select(p => new ProductDto(p.Id, p.Sku, p.Name, p.Price, p.StockQuantity))
            .ToListAsync(cancellationToken);
    }
}
