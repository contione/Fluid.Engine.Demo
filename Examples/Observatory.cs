using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Fluid.Engine.Demo.Configuration;
using Fluid.Engine.Demo.Contracts;
using Fluid.Engine.Demo.Fields;
using Fluid.Engine.Demo.Providers;
using Fluid.Engine.Demo.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fluid.Engine.Demo.Examples;

internal sealed class Observatory : IDisposable
{
    private readonly HttpClient _http = new(new FixtureHandler());
    private readonly TemplatePlanCache _cache;
    private readonly ConcurrentQueue<string> _calls = new();
    public RenderService Service { get; }

    public Observatory()
    {
        var endpoint = Options.Create(new StationHttpOptions { BaseUrl = "https://observatory.invalid/" });
        using var config = (ConfigurationRoot) new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json").Build();
        var settings = Options.Create(config.GetRequiredSection(RenderEngineOptions.SectionName).Get<RenderEngineOptions>()
            ?? throw new InvalidOperationException("Missing render engine settings."));
        IDataProvider[] providers = [new StationProvider(_http, endpoint), new StationJsonProvider(_http, endpoint)];
        var registry = new ProviderRegistry(providers.Select(provider => new RecordedProvider(provider, _calls)));
        _cache = new TemplatePlanCache(settings);
        Service = new RenderService(_cache, new TemplateRenderer(), new InMemoryFieldRegistry(settings),
            new FieldValueResolver(registry, NullLogger<FieldValueResolver>.Instance), NullLogger<RenderService>.Instance);
    }

    public async Task<(RenderResponse Response, string[] Calls)> RenderAsync(RenderRequest request)
    {
        if (RequestValidator.Validate(request).Count != 0) throw new ArgumentException("Invalid demo request.", nameof(request));
        _calls.Clear();
        var response = await Service.RenderAsync(request, "observation", CancellationToken.None);
        return (response, _calls.ToArray());
    }

    public void Dispose() { _cache.Dispose(); _http.Dispose(); }

    private sealed class RecordedProvider(IDataProvider inner, ConcurrentQueue<string> calls) : IDataProvider
    {
        public string Id => inner.Id;
        public Task<IReadOnlyDictionary<string, FieldValue>> ResolveAsync(IReadOnlyDictionary<string, string?> context,
            IReadOnlyList<FieldDefinition> fields, CancellationToken cancellationToken)
        {
            calls.Enqueue(Id);
            return inner.ResolveAsync(context, fields, cancellationToken);
        }
    }

    private sealed class FixtureHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            const string sample = """{"stationName":"North Ridge","open":false,"note":"Ice & <fog>","products":[{"id":"OPT-01"},{"name":"prism"}],"label":"   ","instruments":[{"name":"lens","id":"L-07"},{"name":"prism"}]}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(sample, Encoding.UTF8, "application/json") });
        }
    }
}
