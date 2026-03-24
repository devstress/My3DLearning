using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseIntegrationPlatform.Security;

/// <summary>
/// Extension methods for registering platform security services.
/// </summary>
public static class SecurityServiceExtensions
{
    /// <summary>
    /// Registers JWT bearer authentication using options bound from the
    /// <c>Jwt</c> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="JwtOptions.SigningKey"/> is null or empty.
    /// </exception>
    public static IServiceCollection AddPlatformJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
            throw new InvalidOperationException(
                "Jwt:SigningKey must be configured. Use a secrets manager in production.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(jwtOptions.Issuer),
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = !string.IsNullOrWhiteSpace(jwtOptions.Audience),
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = jwtOptions.ValidateLifetime,
                    ClockSkew = jwtOptions.ClockSkew,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                };
            });

        return services;
    }

    /// <summary>
    /// Registers the <see cref="IPayloadSizeGuard"/> service that enforces
    /// maximum payload size on inbound messages.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPayloadSizeGuard(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Configure<PayloadSizeOptions>(
            configuration.GetSection(PayloadSizeOptions.SectionName));
        services.AddSingleton<IPayloadSizeGuard, PayloadSizeGuard>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="IInputSanitizer"/> service for string payload sanitization.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInputSanitizer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IInputSanitizer, InputSanitizer>();
        return services;
    }
}
