namespace WorkerService1;

public class SampleTimedWorker : TimedWorkerBase
{
    private readonly IConfiguration _config;
    public SampleTimedWorker(ILogger<SampleTimedWorker> logger, IConfiguration config) : base(logger, 300)
    {
        _config = config;
    }

    public override async Task DoWork(object? state)
    {
        using HttpClient http = new();
        http.BaseAddress = new Uri(_config["WebApiService2"]);
        await http.GetAsync("weather-forecast");
    }
}
