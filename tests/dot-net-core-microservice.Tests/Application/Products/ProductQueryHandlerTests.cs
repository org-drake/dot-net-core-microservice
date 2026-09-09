using dot_net_core_microservice.Application.Products.Queries.GetAllProducts;
using dot_net_core_microservice.Application.Products.Queries.GetProductById;
using dot_net_core_microservice.Domain.Entities;
using dot_net_core_microservice.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_microservice.Tests.Application.Products;

public class ProductQueryHandlerTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetProductByIdQueryHandler_ShouldReturnProductWhenFound()
    {
        using var context = CreateContext();
        context.Products.Add(new Product { Sku = "SKU-1", Name = "Widget", Price = 5m, StockQuantity = 1 });
        await context.SaveChangesAsync(CancellationToken.None);
        var id = context.Products.Single().Id;

        var handler = new GetProductByIdQueryHandler(context);
        var result = await handler.Handle(new GetProductByIdQuery(id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("SKU-1", result!.Sku);
    }

    [Fact]
    public async Task GetProductByIdQueryHandler_ShouldReturnNullWhenNotFound()
    {
        using var context = CreateContext();
        var handler = new GetProductByIdQueryHandler(context);

        var result = await handler.Handle(new GetProductByIdQuery(999), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllProductsQueryHandler_ShouldReturnAllProductsOrderedById()
    {
        using var context = CreateContext();
        context.Products.AddRange(
            new Product { Sku = "SKU-2", Name = "Second", Price = 2m, StockQuantity = 2 },
            new Product { Sku = "SKU-1", Name = "First", Price = 1m, StockQuantity = 1 });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllProductsQueryHandler(context);
        var result = await handler.Handle(new GetAllProductsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("SKU-2", result[0].Sku);
        Assert.Equal("SKU-1", result[1].Sku);
    }

    [Fact]
    public async Task GetAllProductsQueryHandler_ShouldReturnEmptyListWhenNoProducts()
    {
        using var context = CreateContext();
        var handler = new GetAllProductsQueryHandler(context);

        var result = await handler.Handle(new GetAllProductsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
