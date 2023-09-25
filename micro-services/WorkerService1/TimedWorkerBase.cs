using System.Diagnostics;

namespace WorkerService1;

public class TimedWorkerBase : IHostedService, IDisposable
{
    private readonly double _timePeriod;
    private readonly ILogger<TimedWorkerBase> _logger;
    private Timer? _timer = null;

    /// <summary>
    /// TimedWorkerBase
    /// </summary>
    /// <param name="logger">logger</param>
    /// <param name="timePeriod">Time period in seconds</param>
    public TimedWorkerBase(ILogger<TimedWorkerBase> logger, double timePeriod)
    {
        _logger = logger;
        _timePeriod = timePeriod;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{GetType().Name} starting.");

        _timer = new Timer(Execute, null, TimeSpan.Zero, TimeSpan.FromSeconds(_timePeriod));

        return Task.CompletedTask;
    }

    private void Execute(object? state)
    {
        _logger.LogInformation($"{GetType().Name} running.");
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity($"TimedWorker : {GetType().Name}", ActivityKind.Internal);
        activity?.SetTag("worker.name", GetType().Name);
        activity?.SetTag("execution.time", DateTime.UtcNow.ToString());
        DoWork(state).Wait();
    } 

    public virtual Task DoWork(object? state)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{GetType().Name} stopping.");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
