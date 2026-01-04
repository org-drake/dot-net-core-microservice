using dot_net_core_microservice.Models;
using dot_net_core_microservice.Services;
using Microsoft.AspNetCore.Mvc;

namespace dot_net_core_microservice.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly IWeatherForecastService _service;

    public WeatherForecastController(IWeatherForecastService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<WeatherForecast>> Get()
    {
        var forecast = _service.GetForecast();
        return Ok(forecast);
    }
}
