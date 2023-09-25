using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace WebApiProject2.Controllers;

[ApiController]
[Route("weather-forecast")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<WeatherForecast>>> Get()
    {
        string? baggageValue = Activity.Current?.GetBaggageItem("baggage-test-key");
        _logger.LogInformation($"baggage-test-key:{baggageValue}");
        var result = await Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToList());

        return Ok(result);
    }
}