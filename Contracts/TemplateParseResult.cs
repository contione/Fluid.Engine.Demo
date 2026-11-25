namespace Fluid.Engine.Demo.Contracts;

public sealed record TemplateParseResult(
    IReadOnlyList<string> Variables,
    IReadOnlyList<RenderIssue> Errors);
