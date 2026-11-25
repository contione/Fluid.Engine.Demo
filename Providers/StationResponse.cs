using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluid.Engine.Demo.Providers;

// Nullable scalars preserve the distinction between missing data and 0 / false.
public sealed class StationResponse
{
    [JsonIgnore]
    internal JsonElement RawJson { get; set; }

    public string? StationName { get; init; }
    public string? ObservatoryCode { get; init; }
    public string? SiteName { get; init; }
    public string? Region { get; init; }
    public string? TimeZone { get; init; }
    public string? OperationalStatus { get; init; }
    public string? StationId { get; init; }
    public string? Latitude { get; init; }
    public string? Longitude { get; init; }
    public int? AltitudeMeters { get; init; }
    public int? TelescopeCount { get; init; }
    public int? InstrumentCount { get; init; }
    public int? SkyQuality { get; init; }
    public bool? Open { get; init; }
    public bool? RemoteObserving { get; init; }
    public bool? Automated { get; init; }
}
