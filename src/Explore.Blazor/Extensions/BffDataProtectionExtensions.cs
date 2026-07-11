// ABOUTME: Persists the Blazor BFF Data Protection key ring in its Redis resource.
// ABOUTME: Keeps cookie cryptography durable without coupling the BFF to API persistence.

using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

namespace Explore.Blazor.Extensions;

public static class BffDataProtectionExtensions
{
    public const string ApplicationName = "islamu-event";
    public const string KeyRingName = "islamu-event:data-protection-keys";

    public static IServiceCollection AddBffDataProtection(
        this IServiceCollection services,
        string redisConnectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);

        var connection = new Lazy<IConnectionMultiplexer>(
            () => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton(_ => connection.Value);

        services
            .AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToStackExchangeRedis(
                () => connection.Value.GetDatabase(),
                KeyRingName);

        return services;
    }
}
