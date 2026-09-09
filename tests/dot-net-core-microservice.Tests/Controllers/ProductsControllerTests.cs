using dot_net_core_microservice.Application.Products;
using dot_net_core_microservice.Application.Products.Commands.CreateProduct;
using dot_net_core_microservice.Application.Products.Commands.DeleteProduct;
using dot_net_core_microservice.Application.Products.Commands.UpdateProduct;
using dot_net_core_microservice.Application.Products.Queries.GetAllProducts;
using dot_net_core_microservice.Application.Products.Queries.GetProductById;
using dot_net_core_microservice.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace dot_net_core_microservice.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<ISender> _mockSender;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockSender = new Mock<ISender>();
        _controller = new ProductsController(_mockSender.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithProducts()
    {
        var products = new List<ProductDto> { new(1, "SKU-1", "Widget", 9.99m, 10) };
        _mockSender.Setup(s => s.Send(It.IsAny<GetAllProductsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(products, okResult.Value);
    }

    [Fact]
    public async Task GetById_ShouldReturnOkWhenFound()
    {
        var product = new ProductDto(1, "SKU-1", "Widget", 9.99m, 10);
        _mockSender.Setup(s => s.Send(It.Is<GetProductByIdQuery>(q => q.Id == 1), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _controller.GetById(1, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(product, okResult.Value);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFoundWhenMissing()
    {
        _mockSender.Setup(s => s.Send(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        var result = await _controller.GetById(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtActionWithId()
    {
        _mockSender.Setup(s => s.Send(It.IsAny<CreateProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        var command = new CreateProductCommand("SKU-1", "Widget", 9.99m, 10);

        var result = await _controller.Create(command, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(ProductsController.GetById), createdResult.ActionName);
        Assert.Equal(42, createdResult.Value);
    }

    [Fact]
    public async Task Update_ShouldReturnBadRequestWhenIdMismatch()
    {
        var command = new UpdateProductCommand(2, "SKU-1", "Widget", 9.99m, 10);

        var result = await _controller.Update(1, command, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        _mockSender.Verify(s => s.Send(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_ShouldReturnNoContentWhenUpdated()
    {
        _mockSender.Setup(s => s.Send(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var command = new UpdateProductCommand(1, "SKU-1", "Widget", 9.99m, 10);

        var result = await _controller.Update(1, command, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFoundWhenMissing()
    {
        _mockSender.Setup(s => s.Send(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var command = new UpdateProductCommand(1, "SKU-1", "Widget", 9.99m, 10);

        var result = await _controller.Update(1, command, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContentWhenDeleted()
    {
        _mockSender.Setup(s => s.Send(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFoundWhenMissing()
    {
        _mockSender.Setup(s => s.Send(It.IsAny<DeleteProductCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Delete(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
