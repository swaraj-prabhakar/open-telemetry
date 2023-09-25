using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;

namespace WebApiProject1.Controllers;

[ApiController]
[Route("weather-forecast")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

    private readonly ILogger<WeatherForecastController> _logger;

    private readonly IConfiguration _config;

    private readonly StorageQueueService _messageSender1;

    private readonly ServiceBusQueueService _messageSender2;

    public WeatherForecastController(
        ILogger<WeatherForecastController> logger,
        IConfiguration config,
        StorageQueueService messageSender1,
        ServiceBusQueueService messageSender2)
    {
        _logger = logger;
        _config = config;
        _messageSender1 = messageSender1;
        _messageSender2 = messageSender2;
    }

    [HttpGet]
    public async Task<ActionResult<List<WeatherForecast>>> Get()
    {
        var result = await Task.FromResult(Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToList());

        return Ok(result);
    }

    [HttpGet("from-other-service")]
    public async Task<ActionResult<List<WeatherForecast>>> GetFromOtherService()
    {
        Activity.Current?.SetTag("additional-tag", "test-value");
        Activity.Current?.SetBaggage("baggage-test-key", "test-value");

        using HttpClient http = new();
        http.BaseAddress = new Uri(_config["WebApiService2"]);
        var httpResponse = await http.GetAsync("weather-forecast");
        var content = await httpResponse.Content.ReadAsStringAsync();
        return Ok(JsonConvert.DeserializeObject<List<WeatherForecast>>(content));
    }

    [HttpPost("send-message")]
    public async Task<ActionResult> SendMessageToStorageQueue([FromBody] string message)
    {
        await _messageSender1.Enqueue("queue1", message, default);
        return Ok();
    }

    [HttpPost("send-message/service-bus")]
    public async Task<ActionResult> SendMessageToServiceBusQueue([FromBody] string message)
    {
        await _messageSender2.Enqueue("q1", message, default);
        return Ok();
    }
}