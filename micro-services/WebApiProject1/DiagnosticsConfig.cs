using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace WebApiProject1;

public static class DiagnosticsConfig
{
    public const string ServiceName = "web-api1";
    public static readonly ActivitySource ActivitySource = new (ServiceName);
    public static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
}
