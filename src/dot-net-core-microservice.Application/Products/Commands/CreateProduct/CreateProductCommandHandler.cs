using dot_net_core_microservice.Application.Common.Interfaces;
using dot_net_core_microservice.Domain.Entities;
using MediatR;

namespace dot_net_core_microservice.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateProductCommand, int>
{
    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
