using dot_net_core_microservice.Application.Products.Commands.CreateProduct;
using dot_net_core_microservice.Application.Products.Commands.DeleteProduct;
using dot_net_core_microservice.Application.Products.Commands.UpdateProduct;
using dot_net_core_microservice.Domain.Entities;
using dot_net_core_microservice.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace dot_net_core_microservice.Tests.Application.Products;

public class ProductCommandHandlerTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateProductCommandHandler_ShouldAddProductAndReturnId()
    {
        using var context = CreateContext();
        var handler = new CreateProductCommandHandler(context);
        var command = new CreateProductCommand("SKU-1", "Widget", 9.99m, 10);

        var id = await handler.Handle(command, CancellationToken.None);

        var stored = await context.Products.FindAsync(id);
        Assert.NotNull(stored);
        Assert.Equal("SKU-1", stored!.Sku);
        Assert.Equal("Widget", stored.Name);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_ShouldUpdateExistingProduct()
    {
        using var context = CreateContext();
        context.Products.Add(new Product { Sku = "SKU-1", Name = "Widget", Price = 5m, StockQuantity = 1 });
        await context.SaveChangesAsync(CancellationToken.None);
        var id = context.Products.Single().Id;

        var handler = new UpdateProductCommandHandler(context);
        var result = await handler.Handle(new UpdateProductCommand(id, "SKU-2", "Gadget", 15m, 3), CancellationToken.None);

        Assert.True(result);
        var updated = await context.Products.FindAsync(id);
        Assert.Equal("SKU-2", updated!.Sku);
        Assert.Equal("Gadget", updated.Name);
        Assert.Equal(15m, updated.Price);
        Assert.Equal(3, updated.StockQuantity);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_ShouldReturnFalseWhenProductDoesNotExist()
    {
        using var context = CreateContext();
        var handler = new UpdateProductCommandHandler(context);

        var result = await handler.Handle(new UpdateProductCommand(999, "SKU-2", "Gadget", 15m, 3), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteProductCommandHandler_ShouldRemoveExistingProduct()
    {
        using var context = CreateContext();
        context.Products.Add(new Product { Sku = "SKU-1", Name = "Widget", Price = 5m, StockQuantity = 1 });
        await context.SaveChangesAsync(CancellationToken.None);
        var id = context.Products.Single().Id;

        var handler = new DeleteProductCommandHandler(context);
        var result = await handler.Handle(new DeleteProductCommand(id), CancellationToken.None);

        Assert.True(result);
        Assert.Empty(context.Products);
    }

    [Fact]
    public async Task DeleteProductCommandHandler_ShouldReturnFalseWhenProductDoesNotExist()
    {
        using var context = CreateContext();
        var handler = new DeleteProductCommandHandler(context);

        var result = await handler.Handle(new DeleteProductCommand(999), CancellationToken.None);

        Assert.False(result);
    }
}
