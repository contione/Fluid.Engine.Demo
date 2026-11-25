using System.Diagnostics;
using Fluid.Engine.Demo.Contracts;
using Fluid.Engine.Demo.Fields;
using Fluid.Engine.Demo.Providers;

namespace Fluid.Engine.Demo.Rendering;

public sealed class RenderService(
    TemplatePlanCache planCache,
    TemplateRenderer templateRenderer,
    IFieldRegistry fieldRegistry,
    FieldValueResolver valueResolver,
    ILogger<RenderService> logger)
{
    public TemplateParseResult Parse(string html)
    {
        var plan = planCache.GetOrAdd(html);
        return new TemplateParseResult(plan.Placeholders, plan.Errors);
    }

    public async Task<RenderResponse> RenderAsync(
        RenderRequest request, string requestId, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        using var logScope = logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId });
        var templates = new List<BoundTemplate>(request.Items.Count);
        foreach (var item in request.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            templates.Add(new BoundTemplate(item, planCache.GetOrAdd(item.TemplateContent)));
        }

        var fieldKeys = templates.Where(template => template.IsValid)
            .SelectMany(template => template.FieldKeys).Distinct(StringComparer.Ordinal);
        var fields = fieldRegistry.GetFields(fieldKeys);
        foreach (var template in templates.Where(template => template.IsValid))
        {
            foreach (var key in template.FieldKeys.Where(key => !fields.ContainsKey(key)))
                template.Errors.Add(new RenderIssue(IssueCode.UnknownField,
                    "The field key is not registered.", key));
        }

        // Failed items must not trigger downstream calls.
        var fieldsToResolve = templates.Where(template => template.IsValid)
            .SelectMany(template => template.FieldKeys).Distinct(StringComparer.Ordinal)
            .ToDictionary(key => key, key => fields[key], StringComparer.Ordinal);
        var values = await valueResolver.ResolveAsync(request.Context, fieldsToResolve, cancellationToken);

        var results = new List<RenderResult>(templates.Count);
        foreach (var template in templates)
            results.Add(await RenderItemAsync(template, fields, values, cancellationToken));

        foreach (var issue in results.SelectMany(result => result.Errors.Concat(result.Warnings)).Distinct())
            logger.LogWarning("Request {RequestId}; field outcome {Code}; provider {ProviderId}; key {FieldKey}",
                requestId, issue.Code, issue.ProviderId, issue.Key);

        var failedCount = results.Count(result => result.Status == ItemStatus.Failed);
        var status = failedCount == 0 ? BatchStatus.Completed : BatchStatus.Partial;
        if (failedCount == results.Count) status = BatchStatus.Failed;
        logger.LogInformation("Request {RequestId}: rendered {ItemCount} items in {ElapsedMs}ms; status {Status}",
            requestId, results.Count, timer.ElapsedMilliseconds, status);
        return new RenderResponse(requestId, status, results);
    }

    private async ValueTask<RenderResult> RenderItemAsync(
        BoundTemplate template,
        IReadOnlyDictionary<string, FieldDefinition> fields,
        IReadOnlyDictionary<string, FieldValue> values,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = template.Errors.ToList();
        var warnings = new List<RenderIssue>();
        var replacements = new Dictionary<string, FieldEvaluation>(StringComparer.Ordinal);
        if (template.IsValid)
        {
            foreach (var key in template.FieldKeys)
            {
                var evaluation = FieldPolicyEvaluator.Evaluate(fields[key], values[key]);
                if (evaluation.Error is not null) errors.Add(evaluation.Error);
                else replacements[key] = evaluation;
                if (evaluation.Warning is not null) warnings.Add(evaluation.Warning);
            }
        }

        string? html = null;
        if (errors.Count == 0)
        {
            try
            {
                html = await templateRenderer.RenderAsync(template, replacements, cancellationToken);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or ArithmeticException or FormatException or Fluid.FluidException)
            {
                errors.Add(new RenderIssue(IssueCode.InvalidTemplate, "The Liquid template could not be evaluated."));
            }
        }
        var status = ItemStatus.Completed;
        if (errors.Count > 0) status = ItemStatus.Failed;
        else if (warnings.Count > 0) status = ItemStatus.CompletedWithWarnings;

        var item = template.Item;
        return new RenderResult(item.ItemId, item.TemplateId,
            status, html, item.Metadata, errors, warnings);
    }
}
