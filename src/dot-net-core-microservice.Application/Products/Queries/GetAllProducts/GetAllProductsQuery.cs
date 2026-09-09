using MediatR;

namespace dot_net_core_microservice.Application.Products.Queries.GetAllProducts;

public record GetAllProductsQuery : IRequest<List<ProductDto>>;
