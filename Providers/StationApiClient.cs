using System.Net;
using Fluid.Engine.Demo.Configuration;
using Microsoft.Extensions.Options;

namespace Fluid.Engine.Demo.Providers;

internal sealed class StationApiClient
{
    private const string StationIdToken = "{stationId}";
    private const int MaxResponseBytes = 1024 * 1024;

    private readonly HttpClient _httpClient;
    private readonly StationHttpOptions _options;

    public StationApiClient(HttpClient httpClient, IOptions<StationHttpOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<StationHttpResult> GetAsync(
        string stationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            return new StationHttpResult(FieldValueStatus.InvalidContext);
        }

        if (stationId is "." or "..")
        {
            return new StationHttpResult(FieldValueStatus.InvalidContext);
        }

        if (!TryBuildRequestUri(_options, stationId, out var requestUri))
        {
            return new StationHttpResult(FieldValueStatus.ProviderError);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new StationHttpResult(FieldValueStatus.MissingValue);
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return new StationHttpResult(FieldValueStatus.AccessDenied);
            }

            if (!response.IsSuccessStatusCode)
            {
                return new StationHttpResult(FieldValueStatus.ProviderError);
            }

            var payload = await ReadResponseWithLimitAsync(response.Content, cancellationToken).ConfigureAwait(false);
            if (payload is null)
            {
                return new StationHttpResult(FieldValueStatus.ProviderError);
            }

            return new StationHttpResult(FieldValueStatus.Found, payload);
        }
        catch (HttpRequestException)
        {
            return new StationHttpResult(FieldValueStatus.ProviderError);
        }
        catch (IOException)
        {
            return new StationHttpResult(FieldValueStatus.ProviderError);
        }
    }

    private static async Task<byte[]?> ReadResponseWithLimitAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is > MaxResponseBytes)
        {
            return null;
        }

        await using var responseStream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var payload = new MemoryStream((int)Math.Min(content.Headers.ContentLength ?? 4096, MaxResponseBytes));
        var buffer = new byte[81920];
        var totalBytes = 0;

        while (true)
        {
            var bytesRead = await responseStream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            if (bytesRead > MaxResponseBytes - totalBytes)
            {
                return null;
            }

            payload.Write(buffer, 0, bytesRead);
            totalBytes += bytesRead;
        }

        return payload.ToArray();
    }

    private static bool TryBuildRequestUri(
        StationHttpOptions options,
        string stationId,
        out Uri requestUri)
    {
        requestUri = null!;
        if (!TryGetBaseUri(options.BaseUrl, out var baseUri)
            || !IsValidStationPath(options.StationPath))
        {
            return false;
        }

        string escapedStationId;
        try
        {
            escapedStationId = Uri.EscapeDataString(stationId);
        }
        catch (UriFormatException)
        {
            return false;
        }

        var relativePath = options.StationPath.Replace(StationIdToken, escapedStationId, StringComparison.Ordinal);
        var basePath = baseUri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? baseUri
            : new Uri($"{baseUri.AbsoluteUri}/", UriKind.Absolute);

        if (!Uri.TryCreate(basePath, relativePath, out var candidate)
            || candidate.Scheme != baseUri.Scheme
            || !string.Equals(candidate.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase)
            || candidate.Port != baseUri.Port
            || !string.IsNullOrEmpty(candidate.Query)
            || !string.IsNullOrEmpty(candidate.Fragment))
        {
            return false;
        }

        requestUri = candidate;
        return true;
    }

    private static bool TryGetBaseUri(string? baseUrl, out Uri baseUri)
    {
        baseUri = null!;
        if (string.IsNullOrWhiteSpace(baseUrl)
            || !Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var candidate)
            || candidate.Scheme is not ("http" or "https")
            || !string.IsNullOrEmpty(candidate.Query)
            || !string.IsNullOrEmpty(candidate.Fragment)
            || !string.IsNullOrEmpty(candidate.UserInfo))
        {
            return false;
        }

        baseUri = candidate;
        return true;
    }

    private static bool IsValidStationPath(string? path)
    {
        if (string.IsNullOrEmpty(path)
            || path.StartsWith("/", StringComparison.Ordinal)
            || path.Contains("\\", StringComparison.Ordinal)
            || path.Contains("?", StringComparison.Ordinal)
            || path.Contains("#", StringComparison.Ordinal)
            || path.Contains("%", StringComparison.Ordinal))
        {
            return false;
        }

        var tokenCount = 0;
        foreach (var segment in path.Split('/', StringSplitOptions.None))
        {
            if (segment is "" or "." or "..")
            {
                return false;
            }

            if (segment == StationIdToken)
            {
                tokenCount++;
                continue;
            }

            if (segment.Contains("{", StringComparison.Ordinal)
                || segment.Contains("}", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return tokenCount == 1;
    }
}

internal readonly record struct StationHttpResult(FieldValueStatus Status, byte[]? Payload = null);
