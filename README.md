# Sample .NET 6 and Angular 15 Projects with OpenTelemetry Integration

This provides documentation for setting up a sample application that integrates OpenTelemetry with .NET 6 and Angular 15. The application uses Docker and Docker Compose for setup, and it sets up OpenTelemetry Collector, Jaeger, Prometheus, Grafana, and other necessary components.

## Prerequisites

Ensure you have the following installed on your system:

- Docker
- Docker Compose
- Node 18.12.1

## Setup OpenTelemetry Collector & Backends

Create a `docker-compose.yml` file with the following contents:

```yaml
version: '3.9'
x-default-logging: &logging
  driver: "json-file"
  options:
    max-size: "5m"
    max-file: "2"

networks:
  default:
    name: micro-services
    driver: bridge

services:

  jaeger:
    image: jaegertracing/all-in-one
    container_name: jaeger
    command:
      - "--memory.max-traces"
      - "10000"
      - "--query.base-path"
      - "/jaeger/ui"
      - "--prometheus.server-url"
      - "http://${PROMETHEUS_ADDR}"
    deploy:
      resources:
        limits:
          memory: 300M
    restart: unless-stopped
    ports:
      - "8080:${JAEGER_SERVICE_PORT}"                    # Jaeger UI
      - "4317"                           # OTLP gRPC default port
    environment:
      - COLLECTOR_OTLP_ENABLED=true
      - METRICS_STORAGE_TYPE=prometheus
    logging: *logging
    

  prometheus:
    image: quay.io/prometheus/prometheus:v2.43.0
    container_name: prometheus
    user: root
    command:
      - --web.console.templates=/etc/prometheus/consoles
      - --web.console.libraries=/etc/prometheus/console_libraries
      - --storage.tsdb.retention.time=1h
      - --config.file=/etc/prometheus/prometheus-config.yaml
      - --storage.tsdb.path=/prometheus
      - --web.enable-lifecycle
      - --web.route-prefix=/
      - --enable-feature=exemplar-storage
    volumes:
      - ./prometheus/prometheus-config.yaml:/etc/prometheus/prometheus-config.yaml
    deploy:
      resources:
        limits:
          memory: 300M
    ports:
      - "${PROMETHEUS_SERVICE_PORT}:${PROMETHEUS_SERVICE_PORT}"
    logging: *logging
    

  grafana:
    image: grafana/grafana:9.4.7
    container_name: grafana
    deploy:
      resources:
        limits:
          memory: 100M
    volumes:
      - ./grafana/grafana.ini:/etc/grafana/grafana.ini
      - ./grafana/provisioning/:/etc/grafana/provisioning/
    ports:
      - "${GRAFANA_SERVICE_PORT}"
    logging: *logging
    

  otelcol:
    image: otel/opentelemetry-collector-contrib:0.75.0
    container_name: otel-col
    deploy:
      resources:
        limits:
          memory: 125M
    restart: unless-stopped
    command: [ "--feature-gates=service.connectors", "--config=/etc/otelcol-config.yml", "--config=/etc/otelcol-config-extras.yml" ]
    volumes:
      - ./otelcollector/otelcol-config.yml:/etc/otelcol-config.yml
      - ./otelcollector/otelcol-config-extras.yml:/etc/otelcol-config-extras.yml
    ports:
      - "4317:4317"          # OTLP over gRPC receiver
      - "4318:4318"     # OTLP over HTTP receiver
      - "9464"          # Prometheus exporter
      - "8888"          # metrics endpoint
    depends_on:
      - jaeger
    logging: *logging
```

## Integrate OpenTelemetry with .NET 6

Add the necessary OpenTelemetry NuGet packages to your .NET 6 project. The versions may vary, so please update accordingly:

```xml
<PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.4.0" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.4.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.4.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.0.0-rc9.14" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.1.0-rc.2" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.0.0-rc9.14" />
```

Create a `DiagnosticsConfig.cs` class:

```csharp
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace WebApiProject1
{
    public static class DiagnosticsConfig
    {
        public const string ServiceName = "web-api1";
        public static readonly ActivitySource ActivitySource = new (ServiceName);
        public static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    }
}
```

In your `Program.cs`, add the OpenTelemetry configuration to the services:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
            .AddService(DiagnosticsConfig.ServiceName))
    .WithTracing(providerBuilder => {
        providerBuilder
            .AddSource(DiagnosticsConfig.ActivitySource.Name)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (builder.Configuration["OtelExporter"] == "console")
        {
            providerBuilder.AddConsoleExporter();
        }
        else
        {
            providerBuilder.AddOtlpExporter();
        }
    })
    .WithMetrics(providerBuilder => {
        providerBuilder
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddHttpClientInstrumentation();

        if (builder.Configuration["OtelExporter"] == "console")
        {
            providerBuilder.AddConsoleExporter();
        }
        else
        {
            providerBuilder.AddOtlpExporter();
        }
    });
```

## Integrate with WorkerService

To trace the Worker service execution, you need to start the activity manually for each execution. Modify your Worker class as follows:

```csharp
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
```

## Integrate with Queue

To propagate trace context through queues, you'll need to transfer the trace-parent-id and other attributes manually.

### Integration with Service Bus Queue

Inject the trace context into the application properties of the ServiceBusMessage:

```csharp
private void InjectTraceContextIntoApplicationProperties(ServiceBusMessage message, string key, string value)
{
    message.ApplicationProperties[key] = value;
}
```

Publish a message to the Service Bus queue:

```csharp
public async Task Enqueue(string queueName, string message, CancellationToken ct)
{
    // ...

    var msg = new ServiceBusMessage(message);

    ActivityContext contextToInject = default;
    if (activity != null)
    {
        contextToInject = activity.Context;
    }
    else if (Activity.Current != null)
    {
        contextToInject = Activity.Current.Context;
    }
    // Injecting current activity context into service bus message's application properties
    DiagnosticsConfig.Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), msg, InjectTraceContextIntoApplicationProperties);

    await sender.SendMessageAsync(msg);
}
```

### Extract trace context from Application Properties of ServiceBusMessage

```csharp
private IEnumerable<string> ExtractTraceContextFromApplicationProperties(ServiceBusReceivedMessage? message, string key)
{
    // ...
}
```

### Consume Message from Service Bus Queue

```csharp
async Task MessageHandler(ProcessMessageEventArgs args)
{
    // ...
    
    // extract activity context from Application properties of ServiceBusMessage
    var parentContext = DiagnosticsConfig.Propagator.Extract(default, args.Message, ExtractTraceContextFromApplicationProperties);
    Baggage.Current = parentContext.Baggage;
    
    // start an activity with extracted activity context as parent
    using var activity = DiagnosticsConfig.ActivitySource.StartActivity($"Dequeue : {args.FullyQualifiedNamespace}", ActivityKind.Consumer, parentContext.ActivityContext);
    activity?.SetTag("queue.name", args.FullyQualifiedNamespace);
    activity?.SetTag("queue.dequeue.time", DateTime.UtcNow.ToString());

    await args.CompleteMessageAsync(args.Message);
}
```

## Integration with Storage Queue

Since Azure Storage Queue does not support setting metadata for each message, you can create a custom `Message` class to send trace context to the queue:

```csharp
public class Message
{
    public Dictionary<string, object> Attributes { get; set; }
    public string Data { get; set; }
}
```

Run All Services in Docker Containers

To run the entire setup in Docker containers, follow these steps:

1. Open a command prompt or PowerShell in the root folder.
2. Run the command `docker-compose up -d --build`.

The various services will be accessible at the following URLs:

- Web-API1: http://localhost:9901/swagger/index.html
- Web-API1: http://localhost:9902/swagger/index.html
- Jaeger (traces): http://localhost:8080
- Prometheus (metrics): http://localhost:9090
- Grafana: http://localhost:3000

## Integrate OpenTelemetry with Angular 15

Add OpenTelemetry npm packages to your Angular 15 project:

```json
"@opentelemetry/api": "^1.4.1",
"@opentelemetry/auto-instrumentations-web": "^0.32.1",
"@opentelemetry/context-zone-peer-dep": "^1.12.0",
"@opentelemetry/exporter-trace-otlp-http": "^0.38.0",
"@opentelemetry/instrumentation": "^0.38.0",
"@opentelemetry/sdk-trace-base": "^1.12.0",
"@opentelemetry/sdk-trace-web": "^1.12.0",
"@opentelemetry/semantic-conventions": "^1.12.0",
```

Configure and integrate OpenTelemetry in your Angular project:

1. Create an `otel.ts` file:

```typescript
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { WebTracerProvider, BatchSpanProcessor } from '@opentelemetry/sdk-trace-web';
import { getWebAutoInstrumentations } from '@opentelemetry/auto-instrumentations-web';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { ZoneContextManager } from '@opentelemetry/context-zone-peer-dep';
import { Resource } from '@opentelemetry/resources';
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions';
 
const provider = new WebTracerProvider({
    resource: new Resource({
        [SemanticResourceAttributes.SERVICE_NAME]: 'spa-client'
    })
});
 
provider.addSpanProcessor(
    new BatchSpanProcessor(
        new OTLPTraceExporter({
            url: 'http://localhost:4318/v1/traces'
        }),
    ),
);
 
provider.register({
  contextManager: new ZoneContextManager(),
});
 
 
registerInstrumentations({
    instrumentations: [
        getWebAutoInstrumentations({
            '@opentelemetry/instrumentation-xml-http-request': {
                propagateTraceHeaderCorsUrls: /.*/
            },
            '@opentelemetry/instrumentation-document-load': {},
            '@opentelemetry/instrumentation-fetch': {},
            '@opentelemetry/instrumentation-user-interaction': {}
        }),
    ],
});
```

2. Import the `otel.ts` file in your `main.ts`:

```typescript
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';
import './otel';

platformBrowserDynamic().bootstrapModule(AppModule)
  .catch(err => console.error(err));
```

## Running the Angular Application

To run the Angular application, follow these steps:

1. Open a command prompt or PowerShell in the root folder.
2. Run `npm install --legacy-peer-deps`.
3. Run `ng s --o`.

## References

- [OpenTelemetry](https://opentelemetry.io/docs/what-is-opentelemetry/)
- [.NET Examples](https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/examples)
- [Service Bus Integration](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=connection-string)
- [Storage Queue Integration](https://learn.microsoft.com/en-us/azure/storage/queues/storage-dotnet-how-to-use-queues?tabs=dotnet)
- [Angular-OpenTelemetry](https://timdeschryver.dev/blog/adding-opentelemetry-to-an-angular-application)