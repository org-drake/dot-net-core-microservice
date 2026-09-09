using MediatR;

namespace dot_net_core_microservice.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(int Id) : IRequest<bool>;
