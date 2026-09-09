namespace dot_net_core_microservice.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
}
