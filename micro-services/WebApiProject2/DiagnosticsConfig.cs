using System.Diagnostics;

namespace WebApiProject2;

public static class DiagnosticsConfig
{
    public const string ServiceName = "web-api2";
    public static readonly ActivitySource ActivitySource = new (ServiceName);
}
