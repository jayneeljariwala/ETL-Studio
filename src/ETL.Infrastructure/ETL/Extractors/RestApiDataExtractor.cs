using System.Text.Json;
using ETL.Application.ETL.Abstractions;
using ETL.Domain.Common;
using ETL.Domain.Enums;
using ETL.Infrastructure.ETL.Configuration;
using Microsoft.Extensions.Http;

namespace ETL.Infrastructure.ETL.Extractors;

public sealed class RestApiDataExtractor : IDataExtractor
{
    private readonly IHttpClientFactory _httpClientFactory;

    public RestApiDataExtractor(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public DataSourceType SourceType => DataSourceType.RestApi;

    public async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ExtractAsync(
        string sourceConfigurationJson,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var config = ConfigurationParser.ParseRequired<RestApiSourceConfiguration>(sourceConfigurationJson, "REST API source");
        if (string.IsNullOrWhiteSpace(config.Url))
        {
            throw new DomainException("REST API source url is required.");
        }

        var client = _httpClientFactory.CreateClient(nameof(RestApiDataExtractor));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(5, config.TimeoutSeconds));

        using var request = new HttpRequestMessage(new HttpMethod(config.Method), config.Url);
        foreach (var header in config.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = document.RootElement;
        var array = root.ValueKind switch
        {
            JsonValueKind.Array => root,
            JsonValueKind.Object when !string.IsNullOrWhiteSpace(config.RootArrayProperty) &&
                                      root.TryGetProperty(config.RootArrayProperty, out var nestedArray) => nestedArray,
            _ => throw new DomainException("REST response must be an array or specify RootArrayProperty for array payload.")
        };

        foreach (var item in array.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in item.EnumerateObject())
            {
                row[property.Name] = ConvertJsonValue(property.Value);
            }

            yield return row;
            await Task.Yield();
        }
    }

    private static object? ConvertJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.GetRawText()
        };
    }
}
