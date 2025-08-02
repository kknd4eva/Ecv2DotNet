using Microsoft.Extensions.DependencyInjection;

namespace Ecv2DotNet;

/// <summary>
/// Extension methods for configuring ECv2 validation services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ECv2 signature validation services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="issuerId">The issuer ID for validation</param>
    /// <param name="configureOptions">Optional configuration for additional options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEcv2Validation(
        this IServiceCollection services,
        string issuerId,
        Action<Ecv2Options>? configureOptions = null)
    {
        if (string.IsNullOrWhiteSpace(issuerId))
        {
            throw new ArgumentException("Issuer ID cannot be null or empty", nameof(issuerId));
        }

        services.Configure<Ecv2Options>(options =>
        {
            options.IssuerId = issuerId;
            configureOptions?.Invoke(options);
        });

        services.AddHttpClient<IEcv2Validator, Ecv2Validator>();
        services.AddScoped<IEcv2Validator, Ecv2Validator>();

        return services;
    }

    /// <summary>
    /// Adds ECv2 signature validation services with custom public key URL
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="issuerId">The issuer ID for validation</param>
    /// <param name="publicKeyUrl">Custom URL for Google's public keys</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddEcv2Validation(
        this IServiceCollection services,
        string issuerId,
        string publicKeyUrl)
    {
        return services.AddEcv2Validation(issuerId, options =>
        {
            options.PublicKeyUrl = publicKeyUrl;
        });
    }
}