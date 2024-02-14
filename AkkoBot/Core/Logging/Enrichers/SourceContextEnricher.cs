using Serilog.Core;
using Serilog.Events;

namespace AkkoBot.Core.Logging.Enrichers;

/// <summary>
/// Changes the SourceContext Serilog property from the full assembly name to the regular name of a type .
/// </summary>
internal sealed class SourceContextEnricher : ILogEventEnricher
{
    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var property))
            return;

        var fullName = (property as ScalarValue)?.Value as string ?? string.Empty;
        var lastPeriodIndex = fullName.LastIndexOf('.');
        var name = (lastPeriodIndex is -1 || lastPeriodIndex == fullName.Length - 1)
            ? fullName
            : fullName[(lastPeriodIndex + 1)..];

        logEvent.AddOrUpdateProperty(new LogEventProperty("SourceContext", new ScalarValue(name)));
    }
}