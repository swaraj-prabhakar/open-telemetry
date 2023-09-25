using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WorkerService1;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctxt, services) =>
    {
        services.AddScoped<StorageQueueService>();
        services.AddScoped<ServiceBusQueueService>();
        services.AddHostedService<SampleWorker1>();
        services.AddHostedService<SampleWorker2>();
        services.AddHostedService<SampleTimedWorker>();

        #region openTelemetry

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                    .AddService(DiagnosticsConfig.ServiceName))
            .WithTracing(providerBuilder =>
            {
                providerBuilder
                    .AddSource(DiagnosticsConfig.ActivitySource.Name)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (ctxt.Configuration["OtelExporter"] == "console")
                {
                    providerBuilder.AddConsoleExporter();
                }
                else
                {
                    providerBuilder.AddOtlpExporter();
                }
            })
            .WithMetrics(providerBuilder =>
             {
                 providerBuilder
                     .AddAspNetCoreInstrumentation()
                     .AddRuntimeInstrumentation()
                     .AddHttpClientInstrumentation();

                 if (ctxt.Configuration["OtelExporter"] == "console")
                 {
                     providerBuilder.AddConsoleExporter();
                 }
                 else
                 {
                     providerBuilder.AddOtlpExporter();
                 }
             });

        #endregion
    })
    .Build();


await host.RunAsync();
