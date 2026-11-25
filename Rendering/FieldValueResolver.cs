using System.Collections.Concurrent;
using Fluid.Engine.Demo.Fields;
using Fluid.Engine.Demo.Providers;

namespace Fluid.Engine.Demo.Rendering;

public sealed class FieldValueResolver(ProviderRegistry providerRegistry, ILogger<FieldValueResolver> logger)
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

        var groups = eligibleFields.GroupBy(field => field.ProviderId, StringComparer.Ordinal);
        await Task.WhenAll(groups.Select(group => ResolveGroupAsync(group.Key, group.ToArray(), context, results, cancellationToken)));

        cancellationToken.ThrowIfCancellationRequested();
        return results;
    }

    private static bool HasInvalidContext(FieldDefinition field, IReadOnlyDictionary<string, string?> context) =>
        field.RequiredContext.Any(key => !context.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value));

    private async Task ResolveGroupAsync(string providerId, FieldDefinition[] fields,
        IReadOnlyDictionary<string, string?> context, ConcurrentDictionary<string, FieldValue> results,
        CancellationToken cancellationToken)
    {
        try
        {
            var values = await providerRegistry.Get(providerId).ResolveAsync(context, fields, cancellationToken);
            foreach (var field in fields)
            {
                results[field.Key] = values.TryGetValue(field.Key, out var value) && value is not null ? value : FieldValue.Failed();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning("Provider {ProviderId} failed with {ErrorType}", providerId, exception.GetType().Name);
            foreach (var field in fields)
            {
                results[field.Key] = FieldValue.Failed();
            }
        }
    }
}
