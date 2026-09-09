namespace dot_net_core_microservice.Application.Products;

public record ProductDto(int Id, string Sku, string Name, decimal Price, int StockQuantity);
