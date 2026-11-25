using System.Text.Encodings.Web;
using System.Text.Json;
using Fluid;
using Fluid.Filters;

namespace Fluid.Engine.Demo.Rendering;

public sealed class TemplateRenderer
{
    private static readonly TemplateOptions Options = CreateOptions();

    private static TemplateOptions CreateOptions()
    {
        var options = new TemplateOptions { MaxSteps = 10_000, StrictFilters = true, ModelNamesComparer = StringComparer.Ordinal };
        options.Filters.WithMoneyFilters();
        return options;
    }

    public async ValueTask<string> RenderAsync(BoundTemplate template,
        IReadOnlyDictionary<string, FieldEvaluation> values, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!template.IsValid || template.Plan.CompiledTemplate is not { } compiled)
            throw new InvalidOperationException("Cannot render an invalid template.");
        var model = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var path in template.Plan.Placeholders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = template.Item.Attributes is null ? path : template.Item.Attributes[path];
            var value = values[key];
            SetPath(model, path, value.StructuredValue is { } json ? ToModel(json) : value.Value);
        }
        var context = new TemplateContext(Options);
        foreach (var (key, value) in model) context.SetValue(key, value);
        var result = await compiled.RenderAsync(context, HtmlEncoder.Default);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    private static void SetPath(Dictionary<string, object?> model, string path, object? value)
    {
        var parts = path.Split('.');
        foreach (var part in parts.AsSpan(0, parts.Length - 1))
        {
            if (!model.TryGetValue(part, out var child)) model[part] = child = new Dictionary<string, object?>(StringComparer.Ordinal);
            model = child as Dictionary<string, object?> ?? throw new InvalidOperationException("Conflicting Liquid bindings.");
        }
        if (!model.TryAdd(parts[^1], value)) throw new InvalidOperationException("Conflicting Liquid bindings.");
    }

    private static object? ToModel(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Array => value.EnumerateArray().Select(ToModel).ToArray(),
        JsonValueKind.Object => value.EnumerateObject().ToDictionary(property => property.Name,
            property => ToModel(property.Value), StringComparer.Ordinal),
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
        JsonValueKind.Number => throw new InvalidOperationException("Liquid number exceeds decimal precision."),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };
}
