using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace Babylon.Alfred.Mcp.Logging;

// The MCP SDK logs "{EndpointName} method '{Method}' ..." where EndpointName is
// "Server (Babylon.Alfred.Mcp 1.0.0.0)". We don't control that template, so we
// rewrite the EndpointName property before it's rendered.
public class EndpointNameVersionEnricher : ILogEventEnricher
{
    private static readonly Regex ParenthesizedSuffix = new(@"\s\(.*\)$", RegexOptions.Compiled);

    public static string StripName(string endpointName) =>
        ParenthesizedSuffix.Replace(endpointName, string.Empty);

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("EndpointName", out var value) &&
            value is ScalarValue { Value: string endpointName })
        {
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("EndpointName", StripName(endpointName)));
        }
    }
}
