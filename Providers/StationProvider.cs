using System.Globalization;
using System.Text.Json;
using Fluid.Engine.Demo.Configuration;
using Fluid.Engine.Demo.Fields;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fluid.Engine.Demo.Providers;

public sealed class StationProvider : KeyedDataProviderBase<string, StationProviderData>
{
    public const string ProviderId = "stationProvider";

    private readonly StationApiClient _apiClient;

    public StationProvider(HttpClient httpClient, IOptions<StationHttpOptions> options)
        : this(httpClient, options, NullLogger<StationProvider>.Instance)
    {
    }

    [ActivatorUtilitiesConstructor]
    public StationProvider(
        HttpClient httpClient,
        IOptions<StationHttpOptions> options,
        ILogger<StationProvider> logger) : base(logger)
    {
        _apiClient = new StationApiClient(httpClient, options);
    }

    public override string Id => ProviderId;

    protected override string? GetKeyFromContext(IReadOnlyDictionary<string, string?> context) =>
        context.TryGetValue("stationId", out var key) && !string.IsNullOrWhiteSpace(key) ? key : null;

    protected override async Task<StationProviderData?> FetchAsync(
        string key,
        IReadOnlyDictionary<string, string?> context,
        CancellationToken cancellationToken)
    {
        var response = await _apiClient.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (response.Status != FieldValueStatus.Found)
        {
            return new StationProviderData(response.Status);
        }

        StationResponse? station;
        try
        {
            station = JsonSerializer.Deserialize(response.Payload!, StationJsonContext.Default.StationResponse);
            if (station is not null)
            {
                using var document = JsonDocument.Parse(response.Payload!);
                station.RawJson = document.RootElement.Clone();
            }
        }
        catch (JsonException)
        {
            return new StationProviderData(FieldValueStatus.ProviderError);
        }

        if (station is null)
        {
            return new StationProviderData(FieldValueStatus.ProviderError);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new StationProviderData(FieldValueStatus.Found, station);
    }

    protected override FieldValue MapField(StationProviderData? data, FieldDefinition field) =>
        data!.Status == FieldValueStatus.Found
            ? ReadField(data.Station!, field)
            : new FieldValue(data.Status);

    // Explicit accessors give compile-time checking without reflection or JSON path traversal.
    private static FieldValue ReadField(
        StationResponse station,
        FieldDefinition field)
    {
        var typedValue = field.SourceField switch
        {
            "stationName" => Value(station.StationName),
            "observatoryCode" => Value(station.ObservatoryCode),
            "siteName" => Value(station.SiteName),
            "region" => Value(station.Region),
            "timeZone" => Value(station.TimeZone),
            "operationalStatus" => Value(station.OperationalStatus),
            "stationId" => Value(station.StationId),
            "latitude" => Value(station.Latitude),
            "longitude" => Value(station.Longitude),
            "altitudeMeters" => Value(station.AltitudeMeters, field.AllowStructuredValues),
            "telescopeCount" => Value(station.TelescopeCount, field.AllowStructuredValues),
            "instrumentCount" => Value(station.InstrumentCount, field.AllowStructuredValues),
            "skyQuality" => Value(station.SkyQuality, field.AllowStructuredValues),
            "open" => Value(station.Open, field.AllowStructuredValues),
            "remoteObserving" => Value(station.RemoteObserving, field.AllowStructuredValues),
            "automated" => Value(station.Automated, field.AllowStructuredValues),
            _ => FieldValue.Failed()
        };

        if (!field.IsInferred || typedValue.Status != FieldValueStatus.ProviderError)
            return typedValue;

        var readResult = JsonFieldReader.Read(
            station.RawJson,
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

    private static FieldValue Value(string? value) =>
        value is null ? FieldValue.Missing() : FieldValue.Found(value);

    private static FieldValue Value(int? value, bool allowStructuredValues) => value is null
        ? FieldValue.Missing()
        : allowStructuredValues
            ? FieldValue.Found(JsonSerializer.SerializeToElement(value.Value),
                value.Value.ToString(CultureInfo.InvariantCulture))
            : FieldValue.Found(value.Value.ToString(CultureInfo.InvariantCulture));

    private static FieldValue Value(bool? value, bool allowStructuredValues) => value switch
    {
        true when allowStructuredValues => FieldValue.Found(JsonSerializer.SerializeToElement(true), "true"),
        false when allowStructuredValues => FieldValue.Found(JsonSerializer.SerializeToElement(false), "false"),
        true => FieldValue.Found("true"),
        false => FieldValue.Found("false"),
        null => FieldValue.Missing()
    };
}

public sealed record StationProviderData(FieldValueStatus Status, StationResponse? Station = null);
