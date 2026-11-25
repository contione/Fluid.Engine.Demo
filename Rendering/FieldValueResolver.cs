using System.Collections.Concurrent;
using Fluid.Engine.Demo.Fields;
using Fluid.Engine.Demo.Providers;

namespace Fluid.Engine.Demo.Rendering;

public sealed class FieldValueResolver(
    ProviderRegistry providerRegistry,
    ILogger<FieldValueResolver> logger)
{
    public async Task<IReadOnlyDictionary<string, FieldValue>> ResolveAsync(
        IReadOnlyDictionary<string, string?> context,
        IReadOnlyDictionary<string, FieldDefinition> fields,
        CancellationToken cancellationToken)
    {
        var results = new ConcurrentDictionary<string, FieldValue>(StringComparer.Ordinal);
        var eligibleFields = new List<FieldDefinition>();
        foreach (var field in fields.Values)
        {
            if (HasInvalidContext(field, context))
                results[field.Key] = new FieldValue(FieldValueStatus.InvalidContext);
            else eligibleFields.Add(field);
        }

        await Task.WhenAll(eligibleFields.GroupBy(field => field.ProviderId, StringComparer.Ordinal)
            .Select(async group =>
            {
                var requestedFields = group.ToArray();
                try { await ResolveGroupAsync(group.Key, requestedFields, context, results, cancellationToken); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    // Downstream exception messages/URLs may contain request identifiers.
                    logger.LogWarning("Provider {ProviderId} failed with {ErrorType}",
                        group.Key, exception.GetType().Name);
                    foreach (var field in requestedFields)
                        results[field.Key] = FieldValue.Failed();
                }
            }));

        cancellationToken.ThrowIfCancellationRequested();
        return results;
    }

    private static bool HasInvalidContext(FieldDefinition field, IReadOnlyDictionary<string, string?> context) =>
        field.RequiredContext.Any(key => !context.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value));

    private async Task ResolveGroupAsync(string providerId, FieldDefinition[] fields,
        IReadOnlyDictionary<string, string?> context, ConcurrentDictionary<string, FieldValue> results,
        CancellationToken cancellationToken)
    {
        var values = await providerRegistry.Get(providerId).ResolveAsync(context, fields, cancellationToken);
        foreach (var field in fields)
            results[field.Key] = values.TryGetValue(field.Key, out var value) && value is not null ? value : FieldValue.Failed();
    }
}
