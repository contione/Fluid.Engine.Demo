using System.Text.Json;
using System.Text.Json.Nodes;
using Fluid.Engine.Demo.Fields;
using Microsoft.Extensions.Logging;

namespace Fluid.Engine.Demo.Providers;

public abstract class JsonDataProviderBase<TKey>(ILogger logger) : KeyedDataProviderBase<TKey, JsonObject>(logger)
    where TKey : notnull
{
    protected override FieldValue MapField(JsonObject? data, FieldDefinition field)
    {
        if (data is null || !TryResolvePath(data, field.SourceField, out var node))
            return FieldValue.Missing();
        if (node is null)
            return FieldValue.Found(null);
        var allowStructuredValues = field.AllowStructuredValues;
        if ((node is JsonObject or JsonArray) && allowStructuredValues)
            return FieldValue.Found(ToStructuredValue(node));
        if (node is JsonObject or JsonArray)
            return FieldValue.Missing();
        if (node is not JsonValue value)
            return FieldValue.Missing();

        return value.GetValueKind() switch
        {
            JsonValueKind.String when value.TryGetValue<string>(out var text) => FieldValue.Found(text),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False =>
                allowStructuredValues
                    ? FieldValue.Found(ToStructuredValue(value), value.ToJsonString())
                    : FieldValue.Found(value.ToJsonString()),
            _ => FieldValue.Found(null)
        };
    }

    private static JsonElement ToStructuredValue(JsonNode node)
    {
        using var document = JsonDocument.Parse(node.ToJsonString());
        return document.RootElement.Clone();
    }

    private static bool TryResolvePath(JsonObject root, string path, out JsonNode? node)
    {
        node = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (node is null)
                return true;
            if (node is not JsonObject obj || !TryGetProperty(obj, segment, out node))
                return false;
        }

        return true;
    }

    private static bool TryGetProperty(JsonObject obj, string name, out JsonNode? value)
    {
        if (obj.TryGetPropertyValue(name, out value))
            return true;

        foreach (var (key, child) in obj)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = child;
                return true;
            }
        }

        value = null;
        return false;
    }
}
