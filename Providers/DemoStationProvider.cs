using Fluid.Engine.Demo.Fields;

namespace Fluid.Engine.Demo.Providers;

public sealed class DemoStationProvider : IDataProvider
{
    public const string ProviderId = "demo-station";

    public string Id => ProviderId;

    public Task<IReadOnlyDictionary<string, FieldValue>> ResolveAsync(
        IReadOnlyDictionary<string, string?> context,
        IReadOnlyList<FieldDefinition> fields,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(fields);
        cancellationToken.ThrowIfCancellationRequested();

        var values = new Dictionary<string, FieldValue>(StringComparer.Ordinal);
        var isKnownStation = context.TryGetValue("stationId", out var stationId)
            && string.Equals(stationId, "station-demo-001", StringComparison.Ordinal);

        foreach (var field in fields)
        {
            values[field.Key] = isKnownStation
                ? ReadKnownStationField(field.SourceField)
                : FieldValue.Missing();
        }

        return Task.FromResult<IReadOnlyDictionary<string, FieldValue>>(values);
    }

    private static FieldValue ReadKnownStationField(string sourceField) => sourceField switch
    {
        "stationName" => FieldValue.Found("North Ridge Observatory"),
        "siteName" => FieldValue.Found("Summit Dome"),
        "altitudeMeters" => FieldValue.Found("2418"),
        "open" => FieldValue.Found("false"),
        "instrumentCount" => FieldValue.Found("3"),
        "skyQuality" => FieldValue.Found("21"),
        _ => FieldValue.Missing()
    };
}
