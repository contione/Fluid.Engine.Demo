using Fluid.Engine.Demo.Configuration;

namespace Fluid.Engine.Demo.Fields;

public sealed record FieldDefinition(
    string Key,
    string ProviderId,
    string SourceField,
    IReadOnlyList<string> RequiredContext,
    EmptyValueMode EmptyValueMode,
    FailureAction OnMissingValue,
    FailureAction OnInvalidContext,
    FailureAction OnProviderError,
    string? DefaultValue)
{
    internal bool IsInferred { get; init; }
    public bool AllowStructuredValues { get; init; }
}
