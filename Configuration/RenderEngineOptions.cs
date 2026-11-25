namespace Fluid.Engine.Demo.Configuration;

public enum FailureAction { Error, Empty, DefaultValue }

public enum EmptyValueMode { NullOnly, NullOrEmpty, NullOrWhiteSpace }

public sealed class RenderEngineOptions
{
    public const string SectionName = "RenderEngineOptions";
    public TemplateCacheOptions TemplateCache { get; init; } = new();
    public Dictionary<string, FieldOptions> Fields { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, FieldGroupOptions> Groups { get; init; } = new(StringComparer.Ordinal);
}

public sealed class TemplateCacheOptions
{
    public bool Enabled { get; init; } = true;
    public int SizeLimitMiB { get; init; } = 16;
    public int MinSizeLimitMiB { get; init; } = 1;
    public int MaxSizeLimitMiB { get; init; } = 1024;
    public int ExpirationMinutes { get; init; } = 30;
}

public sealed class FieldOptions
{
    public string ProviderId { get; init; } = "";
    public string SourceField { get; init; } = "";
    public string[] RequiredContext { get; init; } = [];
    public bool AllowStructuredValues { get; init; }
    public EmptyValueMode EmptyValueMode { get; init; } = EmptyValueMode.NullOnly;
    public FailureAction OnMissingValue { get; init; } = FailureAction.Error;
    public FailureAction OnInvalidContext { get; init; } = FailureAction.Error;
    public FailureAction OnProviderError { get; init; } = FailureAction.Error;
    public string? DefaultValue { get; init; }
}

public sealed class FieldGroupOptions
{
    public bool AllowUnlistedMembers { get; init; }
    public string ProviderId { get; init; } = "";
    public string[] RequiredContext { get; init; } = [];
    public bool AllowStructuredValues { get; init; }
    public EmptyValueMode EmptyValueMode { get; init; } = EmptyValueMode.NullOnly;
    public FailureAction OnMissingValue { get; init; } = FailureAction.Error;
    public FailureAction OnInvalidContext { get; init; } = FailureAction.Error;
    public FailureAction OnProviderError { get; init; } = FailureAction.Error;
    public string? DefaultValue { get; init; }
    public Dictionary<string, FieldOverrideOptions> Members { get; init; } = new(StringComparer.Ordinal);
}

public sealed class FieldOverrideOptions
{
    public string? SourceField { get; init; }
    public string[]? RequiredContext { get; init; }
    public bool? AllowStructuredValues { get; init; }
    public EmptyValueMode? EmptyValueMode { get; init; }
    public FailureAction? OnMissingValue { get; init; }
    public FailureAction? OnInvalidContext { get; init; }
    public FailureAction? OnProviderError { get; init; }
    public string? DefaultValue { get; init; }
}
