using dot_net_core_microservice.Controllers;
using dot_net_core_microservice.Models;
using dot_net_core_microservice.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace dot_net_core_microservice.Tests.Controllers;

public class WeatherForecastControllerTests
{
    private readonly Mock<IWeatherForecastService> _mockService;
    private readonly WeatherForecastController _controller;

    public WeatherForecastControllerTests()
    {
        _mockService = new Mock<IWeatherForecastService>();
        _controller = new WeatherForecastController(_mockService.Object);
    }

    [Fact]
    public void Get_ShouldReturnOkResultWithForecasts()
    {
        // Arrange
        var forecasts = new List<WeatherForecast>
        {
            new() { Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)), TemperatureC = 20, Summary = "Warm" },
            new() { Date = DateOnly.FromDateTime(DateTime.Now.AddDays(2)), TemperatureC = 25, Summary = "Hot" }
        };
        _mockService.Setup(s => s.GetForecast()).Returns(forecasts);

        // Act
        var result = _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedForecasts = Assert.IsAssignableFrom<IEnumerable<WeatherForecast>>(okResult.Value);
        Assert.Equal(2, returnedForecasts.Count());
    }

    [Fact]
    public void Get_ShouldCallServiceGetForecast()
    {
        // Arrange
        var forecasts = new List<WeatherForecast>();
        _mockService.Setup(s => s.GetForecast()).Returns(forecasts);

        // Act
        _controller.Get();

        // Assert
        _mockService.Verify(s => s.GetForecast(), Times.Once);
    }

    [Fact]
    public void Get_ShouldReturnEmptyListWhenServiceReturnsEmpty()
    {
        // Arrange
        _mockService.Setup(s => s.GetForecast()).Returns(new List<WeatherForecast>());

        // Act
        var result = _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedForecasts = Assert.IsAssignableFrom<IEnumerable<WeatherForecast>>(okResult.Value);
        Assert.Empty(returnedForecasts);
    }
}
