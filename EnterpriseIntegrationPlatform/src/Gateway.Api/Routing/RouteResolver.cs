using EnterpriseIntegrationPlatform.Gateway.Api.Configuration;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationPlatform.Gateway.Api.Routing;

/// <summary>
/// Resolves versioned API routes to downstream service URLs using prefix matching.
/// </summary>
/// <remarks>
/// Supported route patterns:
/// <list type="bullet">
///   <item><c>/api/v{n}/admin/*</c> → Admin.Api</item>
///   <item><c>/api/v{n}/inspect/*</c> → OpenClaw.Web</item>
/// </list>
/// </remarks>
public sealed class RouteResolver : IRouteResolver
{
    private readonly GatewayOptions _options;
    private readonly List<RouteMapping> _mappings;

    /// <summary>
    /// Initializes a new instance of <see cref="RouteResolver"/>.
    /// </summary>
    /// <param name="options">Gateway configuration options.</param>
    public RouteResolver(IOptions<GatewayOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;

        _mappings =
        [
            new RouteMapping("/api/v", "/admin", _options.AdminApiBaseUrl, "/api/admin"),
            new RouteMapping("/api/v", "/inspect", _options.OpenClawBaseUrl, "/api/inspect"),
        ];
    }

    /// <inheritdoc />
    public string? Resolve(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        foreach (var mapping in _mappings)
        {
            if (!path.StartsWith(mapping.VersionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Extract everything after "/api/v" — expect "{version}/{segment}/..."
            var remaining = path[mapping.VersionPrefix.Length..];
            var slashIndex = remaining.IndexOf('/', StringComparison.Ordinal);
            if (slashIndex < 0)
            {
                continue;
            }

            var afterVersion = remaining[slashIndex..]; // e.g. "/admin/status"

            if (!afterVersion.StartsWith(mapping.Segment, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var tail = afterVersion[mapping.Segment.Length..]; // e.g. "/status"
            var downstream = $"{mapping.BaseUrl.TrimEnd('/')}{mapping.DownstreamPrefix}{tail}";
            return downstream;
        }

        return null;
    }

    private sealed record RouteMapping(
        string VersionPrefix,
        string Segment,
        string BaseUrl,
        string DownstreamPrefix);
}
