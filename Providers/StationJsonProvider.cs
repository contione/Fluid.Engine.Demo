using System.Text.Json;
using Fluid.Engine.Demo.Configuration;
using Fluid.Engine.Demo.Fields;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fluid.Engine.Demo.Providers;

// Maps configured field paths against the station API JSON response.
public sealed class StationJsonProvider : KeyedDataProviderBase<string, StationJsonProviderData>
{
    public const string ProviderId = "station-http";

    private readonly StationApiClient _apiClient;

    public StationJsonProvider(HttpClient httpClient, IOptions<StationHttpOptions> options)
        : this(httpClient, options, NullLogger<StationJsonProvider>.Instance)
    {
    }

    [ActivatorUtilitiesConstructor]
    public StationJsonProvider(
        HttpClient httpClient,
        IOptions<StationHttpOptions> options,
        ILogger<StationJsonProvider> logger) : base(logger)
    {
        _apiClient = new StationApiClient(httpClient, options);
    }

    public override string Id => ProviderId;

    protected override string? GetKeyFromContext(IReadOnlyDictionary<string, string?> context) =>
        context.TryGetValue("stationId", out var key) && !string.IsNullOrWhiteSpace(key) ? key : null;

    protected override async Task<StationJsonProviderData?> FetchAsync(
        string key,
        IReadOnlyDictionary<string, string?> context,
        CancellationToken cancellationToken)
    {
        var response = await _apiClient.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (response.Status != FieldValueStatus.Found)
        {
            return new StationJsonProviderData(response.Status);
        }

        try
        {
            using var document = JsonDocument.Parse(response.Payload!);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new StationJsonProviderData(FieldValueStatus.ProviderError);
            }

            return new StationJsonProviderData(FieldValueStatus.Found, document.RootElement.Clone());
        }
        catch (JsonException)
        {
            return new StationJsonProviderData(FieldValueStatus.ProviderError);
        }
    }

    protected override FieldValue MapField(StationJsonProviderData? data, FieldDefinition field)
    {
        if (data!.Status != FieldValueStatus.Found)
        {
            return new FieldValue(data.Status);
        }

        var readResult = JsonFieldReader.Read(
            data.Station,
            field.SourceField,
            field.AllowStructuredValues);
        return readResult.Status switch
        {
            JsonFieldReadStatus.Found when readResult.StructuredValue is { } structuredValue =>
                FieldValue.Found(structuredValue, readResult.Value),
            JsonFieldReadStatus.Found => FieldValue.Found(readResult.Value),
            JsonFieldReadStatus.Missing => FieldValue.Missing(),
            _ => FieldValue.Failed()
        };
    }
}

public sealed record StationJsonProviderData(FieldValueStatus Status, JsonElement Station = default);
