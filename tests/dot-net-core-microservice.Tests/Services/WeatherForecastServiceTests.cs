using dot_net_core_microservice.Models;
using dot_net_core_microservice.Services;

namespace dot_net_core_microservice.Tests.Services;

public class WeatherForecastServiceTests
{
    private readonly WeatherForecastService _service;

    public WeatherForecastServiceTests()
    {
        _service = new WeatherForecastService();
    }

    [Fact]
    public void GetForecast_ShouldReturnFiveForecasts()
    {
        // Act
        var result = _service.GetForecast().ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void GetForecast_ShouldReturnForecastsWithFutureDates()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Act
        var result = _service.GetForecast().ToList();

        // Assert
        Assert.All(result, forecast => Assert.True(forecast.Date > today));
    }

    [Fact]
    public void GetForecast_ShouldReturnForecastsWithValidTemperatureRange()
    {
        // Act
        var result = _service.GetForecast().ToList();

        // Assert
        Assert.All(result, forecast =>
        {
            Assert.InRange(forecast.TemperatureC, -20, 55);
        });
    }

    [Fact]
    public void GetForecast_ShouldReturnForecastsWithSummary()
    {
        // Act
        var result = _service.GetForecast().ToList();

        // Assert
        Assert.All(result, forecast => Assert.NotNull(forecast.Summary));
    }

    [Fact]
    public void GetForecast_ShouldCalculateTemperatureFCorrectly()
    {
        // Act
        var result = _service.GetForecast().ToList();

        // Assert
        Assert.All(result, forecast =>
        {
            var expectedTempF = 32 + (int)(forecast.TemperatureC / 0.5556);
            Assert.Equal(expectedTempF, forecast.TemperatureF);
        });
    }
}
