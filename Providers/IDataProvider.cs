using System.Text.Json;
using Fluid.Engine.Demo.Fields;

namespace Fluid.Engine.Demo.Providers;

public enum FieldValueStatus { Found, MissingValue, InvalidContext, ProviderError, AccessDenied }

public sealed record FieldValue(
    FieldValueStatus Status,
    string? Value = null,
    JsonElement? StructuredValue = null)
{
    public static FieldValue Found(string? value) => new(FieldValueStatus.Found, value);

    public static FieldValue Found(JsonElement structuredValue, string? value = null) =>
        new(FieldValueStatus.Found, value, structuredValue.Clone());

    public static FieldValue Missing() => new(FieldValueStatus.MissingValue);
    public static FieldValue Failed() => new(FieldValueStatus.ProviderError);
}

public interface IDataProvider
{
    string Id { get; }

    // One call per provider for all eligible fields in this station render batch.
    // SourceField extraction belongs to the provider. Return results keyed by Key.
    Task<IReadOnlyDictionary<string, FieldValue>> ResolveAsync(
        IReadOnlyDictionary<string, string?> context,
        IReadOnlyList<FieldDefinition> fields,
        CancellationToken cancellationToken);
}
