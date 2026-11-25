using System.Text.Json;
using Fluid.Engine.Demo.Configuration;
using Fluid.Engine.Demo.Contracts;
using Fluid.Engine.Demo.Fields;
using Fluid.Engine.Demo.Providers;

namespace Fluid.Engine.Demo.Rendering;

public sealed record FieldEvaluation(
    string? Value,
    RenderIssue? Error = null,
    RenderIssue? Warning = null,
    JsonElement? StructuredValue = null);

public static class FieldPolicyEvaluator
{
    public static FieldEvaluation Evaluate(FieldDefinition field, FieldValue result)
    {
        var status = result.Status;
        if (status == FieldValueStatus.Found)
        {
            var missing = result.StructuredValue is null && field.EmptyValueMode switch
            {
                EmptyValueMode.NullOrEmpty => string.IsNullOrEmpty(result.Value),
                EmptyValueMode.NullOrWhiteSpace => string.IsNullOrWhiteSpace(result.Value),
                _ => result.Value is null
            };
            if (!missing)
                return new FieldEvaluation(result.Value, StructuredValue: result.StructuredValue);
            status = FieldValueStatus.MissingValue;
        }

        var (code, action, message) = status switch
        {
            FieldValueStatus.MissingValue => (IssueCode.MissingValue, field.OnMissingValue, "The field has no usable value."),
            FieldValueStatus.InvalidContext => (IssueCode.InvalidContext, field.OnInvalidContext, "Required context is missing or invalid."),
            FieldValueStatus.AccessDenied => (IssueCode.AccessDenied, FailureAction.Error, "The data source denied access."),
            _ => (IssueCode.ProviderError, field.OnProviderError, "The data source could not resolve this field.")
        };

        var issue = new RenderIssue(code, message, field.Key, field.ProviderId);
        return action switch
        {
            FailureAction.Empty => new FieldEvaluation("", Warning: issue),
            FailureAction.DefaultValue => new FieldEvaluation(field.DefaultValue!, Warning: issue),
            _ => new FieldEvaluation(null, Error: issue)
        };
    }
}
