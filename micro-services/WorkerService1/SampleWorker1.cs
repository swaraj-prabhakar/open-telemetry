using System.Diagnostics;

namespace WorkerService1
{
    public class SampleWorker1 : BackgroundService
    {
        private const string QUEUE_NAME = "queue1";
        private readonly ILogger<SampleWorker1> _logger;
        private readonly IServiceProvider _serviceProvider;

        public SampleWorker1(ILogger<SampleWorker1> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var queueService = scope.ServiceProvider.GetRequiredService<StorageQueueService>();

                if (await queueService.MessageExistsAsync("queue1"))
                {
                    _logger.LogInformation($"Message exists");
                    await queueService.Dequeue(QUEUE_NAME, stoppingToken);
                }
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}