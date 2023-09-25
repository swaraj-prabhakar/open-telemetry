using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace WorkerService1;

public static class DiagnosticsConfig
{
    public const string ServiceName = "worker-service1";
    public static readonly ActivitySource ActivitySource = new (ServiceName);
    public static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
}
