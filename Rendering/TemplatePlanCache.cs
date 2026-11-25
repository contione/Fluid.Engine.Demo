using Fluid.Engine.Demo.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Fluid.Engine.Demo.Rendering;

public sealed class TemplatePlanCache : IDisposable
{
    private readonly TemplateCacheOptions _options;
    private readonly MemoryCache _cache;
    private readonly long _sizeLimitBytes;
    private readonly object[] _compileLocks = Enumerable.Range(0, 32).Select(_ => new object()).ToArray();

    public TemplatePlanCache(IOptions<RenderEngineOptions> options)
    {
        _options = options.Value.TemplateCache;
        if (_options.MinSizeLimitMiB <= 0 || _options.MaxSizeLimitMiB < _options.MinSizeLimitMiB)
            throw new OptionsValidationException(RenderEngineOptions.SectionName, typeof(RenderEngineOptions),
                ["TemplateCache requires MinSizeLimitMiB > 0 and MaxSizeLimitMiB >= MinSizeLimitMiB."]);
        if (_options.SizeLimitMiB < _options.MinSizeLimitMiB || _options.SizeLimitMiB > _options.MaxSizeLimitMiB)
            throw new OptionsValidationException(RenderEngineOptions.SectionName, typeof(RenderEngineOptions),
                [$"TemplateCache requires SizeLimitMiB between {_options.MinSizeLimitMiB} and {_options.MaxSizeLimitMiB} MiB."]);
        if (_options.ExpirationMinutes is < 1 or > 1440)
            throw new OptionsValidationException(RenderEngineOptions.SectionName, typeof(RenderEngineOptions),
                ["TemplateCache requires ExpirationMinutes between 1 and 1440."]);

        // A private cache prevents unrelated framework entries from sharing our size budget.
        _sizeLimitBytes = (long)_options.SizeLimitMiB * 1024 * 1024;
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = _sizeLimitBytes });
    }

    public TemplatePlan GetOrAdd(string html) => GetOrAdd(html, LiquidTemplateCompiler.Compile);

    internal TemplatePlan GetOrAdd(string html, Func<string, TemplatePlan> compile)
    {
        if (!_options.Enabled) return compile(html);
        if (_cache.TryGetValue(html, out TemplatePlan? cached)) return cached!;

        // Fixed striped locks coalesce same-template cold starts without an unbounded lock dictionary.
        // Cache hits take no compile lock; unrelated stripes can compile in parallel.
        lock (_compileLocks[(uint)html.GetHashCode(StringComparison.Ordinal) % (uint)_compileLocks.Length])
        {
            if (_cache.TryGetValue(html, out cached)) return cached!;
            var plan = compile(html);
            var size = plan.EstimateSizeBytes(html);
            if (size <= _sizeLimitBytes)
                _cache.Set(html, plan, new MemoryCacheEntryOptions
                {
                    Size = size,
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.ExpirationMinutes)
                });
            return plan;
        }
    }

    public void Clear() => _cache.Compact(1.0);

    public void Remove(string html) => _cache.Remove(html);

    public void Dispose() => _cache.Dispose();
}
