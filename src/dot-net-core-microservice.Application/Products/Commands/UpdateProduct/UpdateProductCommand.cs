using MediatR;

namespace dot_net_core_microservice.Application.Products.Commands.UpdateProduct;

public record UpdateProductCommand(int Id, string Sku, string Name, decimal Price, int StockQuantity) : IRequest<bool>;
