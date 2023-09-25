using System.Diagnostics;

namespace WorkerService1
{
    public class SampleWorker2 : BackgroundService
    {
        private const string QUEUE_NAME = "q1";
        private readonly IServiceProvider _serviceProvider;

        public SampleWorker2(ILogger<SampleWorker2> logger, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var queueService = scope.ServiceProvider.GetRequiredService<ServiceBusQueueService>();

            await queueService.Dequeue(QUEUE_NAME, stoppingToken);
        }
    }
}