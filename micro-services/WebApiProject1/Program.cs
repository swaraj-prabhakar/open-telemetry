using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WebApiProject1;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

#region openTelemetry

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

#endregion

builder.Services.AddTransient<StorageQueueService>();
builder.Services.AddTransient<ServiceBusQueueService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors();

var app = builder.Build();

app.UseCors(opt => opt.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseAuthorization();

app.MapControllers();

app.Run();
