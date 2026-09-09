using MediatR;

namespace dot_net_core_microservice.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(string Sku, string Name, decimal Price, int StockQuantity) : IRequest<int>;
