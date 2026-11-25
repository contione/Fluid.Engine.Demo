using System.Text.Json.Serialization;

namespace Fluid.Engine.Demo.Providers;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(StationResponse))]
internal partial class StationJsonContext : JsonSerializerContext;
