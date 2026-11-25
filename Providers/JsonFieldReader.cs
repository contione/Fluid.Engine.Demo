using System.Text.Json;

namespace Fluid.Engine.Demo.Providers;

internal enum JsonFieldReadStatus
{
    Found,
    Missing,
    ProviderError
}

internal readonly record struct JsonFieldReadResult(
    JsonFieldReadStatus Status,
    string? Value = null,
    JsonElement? StructuredValue = null);

internal static class JsonFieldReader
{
    public static JsonFieldReadResult Read(
        JsonElement root,
        string sourceField,
        bool allowStructuredValues = false)
    {
        if (string.IsNullOrEmpty(sourceField) || root.ValueKind != JsonValueKind.Object)
        {
            return new JsonFieldReadResult(JsonFieldReadStatus.ProviderError);
        }

        var current = root;
        var segments = sourceField.Split('.', StringSplitOptions.None);

        foreach (var segment in segments)
        {
            if (segment.Length == 0)
            {
                return new JsonFieldReadResult(JsonFieldReadStatus.ProviderError);
            }

            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return new JsonFieldReadResult(JsonFieldReadStatus.Missing);
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.Null => new JsonFieldReadResult(JsonFieldReadStatus.Missing),
            JsonValueKind.String => new JsonFieldReadResult(JsonFieldReadStatus.Found, current.GetString() ?? string.Empty),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False =>
                new JsonFieldReadResult(
                    JsonFieldReadStatus.Found,
                    current.GetRawText(),
                    allowStructuredValues ? current.Clone() : null),
            JsonValueKind.Object or JsonValueKind.Array when allowStructuredValues =>
                new JsonFieldReadResult(JsonFieldReadStatus.Found, StructuredValue: current.Clone()),
            JsonValueKind.Object or JsonValueKind.Array => new JsonFieldReadResult(JsonFieldReadStatus.ProviderError),
            _ => new JsonFieldReadResult(JsonFieldReadStatus.ProviderError)
        };
    }
}
