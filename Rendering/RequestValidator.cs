using Fluid.Engine.Demo.Contracts;

namespace Fluid.Engine.Demo.Rendering;

public static class RequestValidator
{
    public static Dictionary<string, string[]> Validate(RenderRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (!IsValidContext(request.Context))
            errors["context"] = ["Context must be an object with at most 32 entries; keys <= 128 and values <= 2048 characters."];

        if (request.Items is null || request.Items.Count == 0)
        {
            errors["items"] = ["Provide at least 1 item."];
            return errors;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            if (item is null)
            {
                errors[$"items[{i}]"] = ["An item must be an object."];
                continue;
            }
            if (!IsValidItemId(item.ItemId, ids)) errors[$"items[{i}].itemId"] = ["ItemId must be nonblank, unique in this request, and at most 128 characters."];
            if (item.TemplateContent is null) errors[$"items[{i}].templateContent"] = ["TemplateContent must be a string."];
            else if (!AreValidAttributes(item.Attributes)) errors[$"items[{i}].attributes"] = ["Attributes must map valid placeholder names to field keys."];
        }

        return errors;
    }

    private static bool IsValidContext(Dictionary<string, string?>? context) =>
        context is not null && context.Count <= 32 && context.All(pair =>
            !string.IsNullOrWhiteSpace(pair.Key) && pair.Key.Length <= 128 && pair.Value?.Length <= 2048);

    private static bool IsValidItemId(string itemId, HashSet<string> ids) =>
        !string.IsNullOrWhiteSpace(itemId) && itemId.Length <= 128 && ids.Add(itemId);

    private static bool AreValidAttributes(IReadOnlyDictionary<string, string>? attributes) =>
        attributes is null || attributes.All(pair =>
            !string.IsNullOrWhiteSpace(pair.Key) && pair.Key.Length <= 128 && pair.Key == pair.Key.Trim()
            && !pair.Key.Contains('{') && !pair.Key.Contains('}')
            && !pair.Key.Contains('\r') && !pair.Key.Contains('\n')
            && !string.IsNullOrWhiteSpace(pair.Value) && pair.Value.Length <= 128);
}
