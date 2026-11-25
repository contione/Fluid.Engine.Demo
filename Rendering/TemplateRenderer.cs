using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fluid;

namespace Fluid.Engine.Demo.Rendering;

public sealed class TemplateRenderer
{
    private static readonly TemplateOptions Options = new TemplateOptionsBuilder()
        .WithModelNamesComparer(StringComparer.Ordinal)
        .WithMaxSteps(10_000)
        .WithStrictFilters()
        .WithMoneyFilters()
        .Build();

    public async ValueTask<string> RenderAsync(BoundTemplate template,
        IReadOnlyDictionary<string, FieldEvaluation> values, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!template.IsValid || template.Plan.CompiledTemplate is not { } compiled)
            throw new InvalidOperationException("Cannot render an invalid template.");

        var model = new JsonObject();
        foreach (var path in template.Plan.Placeholders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = template.Item.Attributes is null ? path : template.Item.Attributes[path];
            var field = values[key];
            Bind(model, path, field.StructuredValue is { } json ? json.Deserialize<JsonNode>() : JsonValue.Create(field.Value));
        }

        var context = new TemplateContext(model, Options) { CancellationToken = cancellationToken };
        return await compiled.RenderAsync(context, HtmlEncoder.Default);
    }

    private static void Bind(JsonObject model, string path, JsonNode? value)
    {
        var parts = path.Split('.');
        foreach (var part in parts.AsSpan(0, parts.Length - 1))
        {
            if (!model.ContainsKey(part)) model[part] = new JsonObject();
            model = model[part] as JsonObject ?? throw new InvalidOperationException("Conflicting Liquid bindings.");
        }
        model.Add(parts[^1], value);
    }
}
