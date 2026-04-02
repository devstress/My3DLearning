namespace EnterpriseIntegrationPlatform.Gateway.Api.Routing;

/// <summary>
/// Resolves incoming request paths to downstream service URLs.
/// </summary>
public interface IRouteResolver
{
    /// <summary>
    /// Resolves the given request path to a fully-qualified downstream URL.
    /// </summary>
    /// <param name="path">The incoming request path.</param>
    /// <returns>The downstream URL, or <c>null</c> if no route matches.</returns>
    string? Resolve(string path);
}
