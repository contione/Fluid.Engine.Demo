using Fluid.Engine.Demo.Contracts;
using Fluid;

namespace Fluid.Engine.Demo.Rendering;

internal static class LiquidTemplateCompiler
{
    private static readonly FluidParser Parser = new();

    public static TemplatePlan Compile(string html)
    {
        if (!Parser.TryParse(html, out var template, out _))
            return Invalid("Invalid Liquid syntax.");
        try
        {
            var dependencies = LiquidDependencyCollector.Collect(template);
            return new TemplatePlan(template, dependencies, [], 4L * html.Length + 256L * dependencies.Length);
        }
        catch (NotSupportedException)
        {
            return Invalid("Unsupported Liquid expression, tag, or filter.");
        }
    }

    private static TemplatePlan Invalid(string message) =>
        new TemplatePlan(null, [], [new RenderIssue(IssueCode.InvalidTemplate, message)], 0);
}
