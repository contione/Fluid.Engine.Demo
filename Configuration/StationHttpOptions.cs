namespace Fluid.Engine.Demo.Configuration;

public sealed class StationHttpOptions
{
    public const string SectionName = "Providers:StationHttp";

    public string? BaseUrl { get; init; } = string.Empty;

    public string StationPath { get; init; } = "stations/{stationId}";
}
