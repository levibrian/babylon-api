using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace Babylon.Alfred.Mcp.Logging;

// The MCP SDK logs "{EndpointName} method '{Method}' ..." where EndpointName is
// "Server (Babylon.Alfred.Mcp 1.0.0.0)". We don't control that template, so we
// rewrite the EndpointName property before it's rendered.
public class EndpointNameVersionEnricher : ILogEventEnricher
{
    private static readonly Regex VersionSuffix = new(@"\s\d+(\.\d+){1,3}\)$", RegexOptions.Compiled);

    public static string StripVersion(string endpointName) =>
        VersionSuffix.Replace(endpointName, ")");

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("EndpointName", out var value) &&
            value is ScalarValue { Value: string endpointName })
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("EndpointName", StripVersion(endpointName)));
        }
    }
}
