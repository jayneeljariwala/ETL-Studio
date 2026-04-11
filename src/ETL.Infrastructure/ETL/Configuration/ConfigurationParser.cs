using System.Text.Json;
using ETL.Domain.Common;

namespace ETL.Infrastructure.ETL.Configuration;

internal static class ConfigurationParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T ParseRequired<T>(string json, string name)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new DomainException($"{name} configuration cannot be empty.");
        }

        var model = JsonSerializer.Deserialize<T>(json, JsonOptions);
        if (model is null)
        {
            throw new DomainException($"Unable to parse {name} configuration.");
        }

        return model;
    }
}
