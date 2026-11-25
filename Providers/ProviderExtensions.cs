using Fluid.Engine.Demo.Fields;

namespace Fluid.Engine.Demo.Providers;

internal static class ProviderExtensions
{
    public static IReadOnlyDictionary<string, FieldValue> ToAllFields(
        this FieldValue value,
        IReadOnlyList<FieldDefinition> fields)
    {
        var values = new Dictionary<string, FieldValue>(fields.Count, StringComparer.Ordinal);
        foreach (var field in fields)
        {
            values[field.Key] = value;
        }

        return values;
    }

    public static Guid? GetStationId(this IReadOnlyDictionary<string, string?> context)
    {
        return context.TryGetValue("stationId", out var value)
            && Guid.TryParse(value, out var stationId)
            ? stationId
            : null;
    }
}
