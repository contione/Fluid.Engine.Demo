using System.Collections.Immutable;
using Fluid.Engine.Demo.Contracts;
using Fluid;

namespace Fluid.Engine.Demo.Rendering;

public sealed class TemplatePlan
{
    private readonly long _compiledSizeBytes;

    internal TemplatePlan(IFluidTemplate? compiledTemplate, ImmutableArray<string> dependencies,
        ImmutableArray<RenderIssue> errors, long compiledSizeBytes)
    {
        CompiledTemplate = compiledTemplate;
        Placeholders = dependencies;
        Errors = errors;
        _compiledSizeBytes = compiledSizeBytes;
    }

    internal IFluidTemplate? CompiledTemplate { get; }
    public ImmutableArray<string> Placeholders { get; }
    public ImmutableArray<RenderIssue> Errors { get; }

    internal long EstimateSizeBytes(string html) => 256L + 2L * html.Length + _compiledSizeBytes
        + Placeholders.Sum(name => 32L + 2L * name.Length)
        + Errors.Sum(error => 64L + 2L * error.Message.Length);
}
