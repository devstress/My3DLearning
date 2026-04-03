using System.Text.RegularExpressions;

namespace EnterpriseIntegrationPlatform.Processing.Transform;

/// <summary>
/// Transform step that applies a regex replacement to the payload string.
/// </summary>
/// <remarks>
/// <para>
/// The step uses a compiled <see cref="Regex"/> for performance. The replacement string
/// supports standard .NET substitution patterns (<c>$1</c>, <c>${name}</c>, etc.).
/// </para>
/// <para>
/// A configurable <see cref="Timeout"/> (default 5 seconds) protects against
/// catastrophic backtracking on malicious input.
/// </para>
/// </remarks>
public sealed class RegexReplaceStep : ITransformStep
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    private readonly Regex _regex;
    private readonly string _replacement;

    /// <summary>
    /// Initialises a new <see cref="RegexReplaceStep"/> with the supplied pattern and
    /// replacement string.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <param name="replacement">
    /// The replacement string; may include substitution patterns.
    /// </param>
    /// <param name="regexOptions">
    /// Additional <see cref="RegexOptions"/> (e.g. <see cref="RegexOptions.IgnoreCase"/>).
    /// <see cref="RegexOptions.Compiled"/> is always added.
    /// </param>
    /// <param name="timeout">
    /// Maximum time allowed for the regex evaluation. Defaults to 5 seconds.
    /// </param>
    public RegexReplaceStep(
        string pattern,
        string replacement,
        RegexOptions regexOptions = RegexOptions.None,
        TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(replacement);

        _replacement = replacement;
        _regex = new Regex(
            pattern,
            regexOptions | RegexOptions.Compiled,
            timeout ?? DefaultTimeout);
    }

    /// <inheritdoc />
    public string Name => "RegexReplace";

    /// <inheritdoc />
    public Task<TransformContext> ExecuteAsync(
        TransformContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var replaced = _regex.Replace(context.Payload, _replacement);
        var result = context.WithPayload(replaced);
        result.Metadata[$"Step.{Name}.Applied"] = "true";
        return Task.FromResult(result);
    }
}
