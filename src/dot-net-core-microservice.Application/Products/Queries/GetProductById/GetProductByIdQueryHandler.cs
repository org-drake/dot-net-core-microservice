using dot_net_core_microservice.Application.Common.Interfaces;
using MediatR;

namespace dot_net_core_microservice.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await context.Products.FindAsync([request.Id], cancellationToken);

        return product is null
            ? null
            : new ProductDto(product.Id, product.Sku, product.Name, product.Price, product.StockQuantity);
    }
}
