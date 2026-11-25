using Fluid.Engine.Demo.Configuration;
using Microsoft.Extensions.Options;

namespace Fluid.Engine.Demo.Fields;

public sealed class InMemoryFieldRegistry : IFieldRegistry
{
    private readonly Dictionary<string, FieldDefinition> _fields;
    private readonly Dictionary<string, FieldDefinition> _groupDefaults = new(StringComparer.Ordinal);

    public InMemoryFieldRegistry(IOptions<RenderEngineOptions> options)
    {
        var settings = options.Value;
        var errors = new List<string>();
        var fields = new Dictionary<string, FieldDefinition>(StringComparer.Ordinal);
        foreach (var (key, field) in settings.Fields)
        {
            if (!IsValidKey(key))
                errors.Add($"Field key '{key}' must be nonblank, trimmed, and at most 128 characters.");
            if (key.Contains('.'))
                errors.Add($"Top-level field key '{key}' must be a root name; use a field group for dotted keys.");
            if (fields.ContainsKey(key))
                errors.Add($"Field key '{key}' is configured more than once.");

            var definition = CreateDefinition(key, field);
            ValidateDefinition(definition, errors);
            fields[key] = definition;
        }

        foreach (var (groupKey, group) in settings.Groups)
        {
            if (!IsValidKey(groupKey))
                errors.Add($"Field group key '{groupKey}' must be nonblank, trimmed, and at most 128 characters.");

            ValidateGroupDefaults(groupKey, group, errors);
            if (group.AllowUnlistedMembers)
            {
                if (groupKey.Contains('.')) errors.Add($"Open field group '{groupKey}' must be a root name.");
                var defaults = CreateDefinition(groupKey, group, groupKey, new FieldOverrideOptions());
                ValidateDefinition(defaults, errors);
                _groupDefaults[groupKey] = defaults with { IsInferred = true };
            }
            foreach (var (memberKey, member) in group.Members)
            {
                if (!IsValidKey(memberKey))
                    errors.Add($"Field member key '{groupKey}.{memberKey}' must be nonblank, trimmed, and at most 128 characters.");

                var canonicalKey = $"{groupKey}.{memberKey}";
                if (fields.ContainsKey(canonicalKey))
                    errors.Add($"Field key '{canonicalKey}' collides with another configured field.");

                var definition = CreateDefinition(canonicalKey, group, memberKey, member);
                ValidateDefinition(definition, errors);
                fields[canonicalKey] = definition;
            }
        }

        if (errors.Count > 0)
            throw new OptionsValidationException(RenderEngineOptions.SectionName, typeof(RenderEngineOptions), errors);

        _fields = fields;
    }

    public IReadOnlyDictionary<string, FieldDefinition> GetFields(IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        var fields = new Dictionary<string, FieldDefinition>(StringComparer.Ordinal);
        foreach (var key in keys.Distinct(StringComparer.Ordinal))
        {
            if (_fields.TryGetValue(key, out var field))
            {
                fields.Add(key, field);
                continue;
            }

            var separator = key.IndexOf('.');
            if (separator < 1 || !IsValidKey(key) || !_groupDefaults.TryGetValue(key[..separator], out var defaults)) continue;
            var member = key[(separator + 1)..];
            if (member.Split('.').All(IsValidKey)) fields.Add(key, defaults with { Key = key, SourceField = member });
        }

        return fields;
    }

    private static FieldDefinition CreateDefinition(string key, FieldOptions field) =>
        new(key, field.ProviderId, field.SourceField, Array.AsReadOnly(field.RequiredContext.ToArray()),
            field.EmptyValueMode, field.OnMissingValue, field.OnInvalidContext, field.OnProviderError,
            field.DefaultValue)
        {
            AllowStructuredValues = field.AllowStructuredValues
        };

    private static FieldDefinition CreateDefinition(
        string key,
        FieldGroupOptions group,
        string memberKey,
        FieldOverrideOptions member) =>
        new(key,
            group.ProviderId,
            member.SourceField ?? memberKey,
            Array.AsReadOnly((member.RequiredContext ?? group.RequiredContext).ToArray()),
            member.EmptyValueMode ?? group.EmptyValueMode,
            member.OnMissingValue ?? group.OnMissingValue,
            member.OnInvalidContext ?? group.OnInvalidContext,
            member.OnProviderError ?? group.OnProviderError,
            member.DefaultValue ?? group.DefaultValue)
        {
            AllowStructuredValues = member.AllowStructuredValues ?? group.AllowStructuredValues
        };

    private static void ValidateGroupDefaults(string groupKey, FieldGroupOptions group, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(group.ProviderId))
            errors.Add($"Field group '{groupKey}' requires ProviderId.");
        if (group.RequiredContext.Any(string.IsNullOrWhiteSpace))
            errors.Add($"Field group '{groupKey}' has an invalid RequiredContext entry.");
        if (!Enum.IsDefined(group.EmptyValueMode) || !Enum.IsDefined(group.OnMissingValue)
            || !Enum.IsDefined(group.OnInvalidContext) || !Enum.IsDefined(group.OnProviderError))
            errors.Add($"Field group '{groupKey}' has an invalid policy enum.");
    }

    private static void ValidateDefinition(FieldDefinition field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(field.ProviderId) || string.IsNullOrWhiteSpace(field.SourceField))
            errors.Add($"Field '{field.Key}' requires ProviderId and SourceField.");
        if (field.RequiredContext.Any(string.IsNullOrWhiteSpace))
            errors.Add($"Field '{field.Key}' has an invalid RequiredContext entry.");
        if (!Enum.IsDefined(field.EmptyValueMode) || !Enum.IsDefined(field.OnMissingValue)
            || !Enum.IsDefined(field.OnInvalidContext) || !Enum.IsDefined(field.OnProviderError))
            errors.Add($"Field '{field.Key}' has an invalid policy enum.");
        if ((field.OnMissingValue == FailureAction.DefaultValue
             || field.OnInvalidContext == FailureAction.DefaultValue
             || field.OnProviderError == FailureAction.DefaultValue) && field.DefaultValue is null)
            errors.Add($"Field '{field.Key}' requires DefaultValue for its selected policy.");
    }

    private static bool IsValidKey(string key) =>
        !string.IsNullOrWhiteSpace(key) && key.Length <= 128 && key == key.Trim();
}
