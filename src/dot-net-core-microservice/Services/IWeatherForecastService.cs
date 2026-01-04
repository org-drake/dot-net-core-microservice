using dot_net_core_microservice.Models;

namespace dot_net_core_microservice.Services;

public interface IWeatherForecastService
{
    IEnumerable<WeatherForecast> GetForecast();
}
