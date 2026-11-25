namespace Fluid.Engine.Demo.Providers;

public sealed class ProviderRegistry
{
    private readonly Dictionary<string, IDataProvider> _providers;

    public ProviderRegistry(IEnumerable<IDataProvider> providers)
    {
        var entries = new Dictionary<string, IDataProvider>(StringComparer.Ordinal);
        foreach (var provider in providers)
        {
            if (string.IsNullOrWhiteSpace(provider.Id) || !entries.TryAdd(provider.Id, provider))
                throw new InvalidOperationException("Provider IDs must be nonblank and unique.");
        }
        _providers = entries;
    }

    public IDataProvider Get(string id) => _providers.TryGetValue(id, out var provider)
        ? provider
        : throw new InvalidOperationException($"Provider '{id}' is not registered.");
}
