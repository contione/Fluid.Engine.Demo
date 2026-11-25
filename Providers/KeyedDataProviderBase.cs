using Fluid.Engine.Demo.Fields;
using Microsoft.Extensions.Logging;

namespace Fluid.Engine.Demo.Providers;

/// <summary>
/// Base implementation for providers that fetch one data object for a context key.
/// </summary>
public abstract class KeyedDataProviderBase<TKey, TData>(ILogger logger) : IDataProvider
    where TKey : notnull
{
    public abstract string Id { get; }

    public async Task<IReadOnlyDictionary<string, FieldValue>> ResolveAsync(
        IReadOnlyDictionary<string, string?> context,
        IReadOnlyList<FieldDefinition> fields,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(fields);

        if (fields.Count == 0)
        {
            return new Dictionary<string, FieldValue>(StringComparer.Ordinal);
        }

        var key = GetKeyFromContext(context);
        if (EqualityComparer<TKey>.Default.Equals(key, default))
        {
            return new FieldValue(FieldValueStatus.InvalidContext).ToAllFields(fields);
        }

        try
        {
            var data = await FetchAsync(key!, context, cancellationToken).ConfigureAwait(false);
            if (EqualityComparer<TData>.Default.Equals(data, default))
            {
                return FieldValue.Missing().ToAllFields(fields);
            }

            return fields.ToDictionary(
                field => field.Key,
                field => MapField(data, field),
                StringComparer.Ordinal);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Provider {ProviderId} was cancelled", Id);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Provider {ProviderId} failed", Id);
            return FieldValue.Failed().ToAllFields(fields);
        }
    }

    /// <summary>
    /// Extracts the station identifier when the key type is Guid. String-key providers
    /// should override this method when their context contract uses another identifier.
    /// </summary>
    protected virtual TKey? GetKeyFromContext(IReadOnlyDictionary<string, string?> context)
    {
        if (context.GetStationId() is TKey key)
        {
            return key;
        }

        return default;
    }

    protected abstract Task<TData?> FetchAsync(
        TKey key,
        IReadOnlyDictionary<string, string?> context,
        CancellationToken cancellationToken);

    protected abstract FieldValue MapField(TData? data, FieldDefinition field);
}
