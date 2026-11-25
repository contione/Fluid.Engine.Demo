using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluid.Engine.Demo.Contracts;

public sealed class RenderRequest
{
    public Dictionary<string, string?> Context { get; init; } = new(StringComparer.Ordinal);
    public List<TemplateItem> Items { get; init; } = [];
}

public sealed class TemplateItem
{
    public string ItemId { get; init; } = "";
    public string? TemplateId { get; init; }
    public string TemplateContent { get; init; } = "";

    // Existing template dictionary: placeholder token -> registered field key.
    // For identical names use, for example, { "stationName": "stationName" }.
    public Dictionary<string, string>? Attributes { get; init; }

    // Upstream structural data is passed through without recursive replacement.
    public JsonElement? Metadata { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<BatchStatus>))]
public enum BatchStatus { Completed, Partial, Failed }

[JsonConverter(typeof(JsonStringEnumConverter<ItemStatus>))]
public enum ItemStatus { Completed, CompletedWithWarnings, Failed }

[JsonConverter(typeof(JsonStringEnumConverter<IssueCode>))]
public enum IssueCode
{
    InvalidTemplate,
    UnknownField,
    MissingValue,
    InvalidContext,
    ProviderError,
    AccessDenied
}

public sealed record RenderIssue(
    IssueCode Code,
    string Message,
    string? Key = null,
    string? ProviderId = null);

public sealed record RenderResult(
    string ItemId,
    string? TemplateId,
    ItemStatus Status,
    string? Html,
    JsonElement? Metadata,
    IReadOnlyList<RenderIssue> Errors,
    IReadOnlyList<RenderIssue> Warnings);

public sealed record RenderResponse(
    string RequestId,
    BatchStatus Status,
    IReadOnlyList<RenderResult> Results);
