using dot_net_core_microservice.Application.Common.Interfaces;
using MediatR;

namespace dot_net_core_microservice.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateProductCommand, bool>
{
    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products.FindAsync([request.Id], cancellationToken);
        if (product is null)
        {
            return false;
        }

        product.Sku = request.Sku;
        product.Name = request.Name;
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
