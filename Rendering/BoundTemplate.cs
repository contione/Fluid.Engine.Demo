using Fluid.Engine.Demo.Contracts;

namespace Fluid.Engine.Demo.Rendering;

// Per-item bindings and diagnostics; the compiled plan can be shared across requests.
public sealed class BoundTemplate
{
    public BoundTemplate(TemplateItem item, TemplatePlan plan)
    {
        Item = item;
        Plan = plan;
        Errors = plan.Errors.ToList();
        if (Errors.Count != 0) return;
        foreach (var path in plan.Placeholders)
        {
            if (item.Attributes is null) FieldKeys.Add(path);
            else if (item.Attributes.TryGetValue(path, out var key)) FieldKeys.Add(key);
            else Errors.Add(new RenderIssue(IssueCode.InvalidTemplate,
                "A Liquid field has no corresponding attribute dictionary entry.", path));
        }
    }

    public TemplateItem Item { get; }
    public TemplatePlan Plan { get; }
    public HashSet<string> FieldKeys { get; } = new(StringComparer.Ordinal);
    public List<RenderIssue> Errors { get; }
    public bool IsValid => Errors.Count == 0;
}
