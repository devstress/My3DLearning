using System.Text.Json;

namespace EnterpriseIntegrationPlatform.Contracts;

/// <summary>
/// Helper methods for appending and reading <see cref="MessageHistoryEntry"/> records
/// from the <see cref="IntegrationEnvelope{T}.Metadata"/> dictionary.
/// </summary>
public static class MessageHistoryHelper
{
    /// <summary>
    /// The metadata key under which the JSON-serialised history chain is stored.
    /// </summary>
    public const string MetadataKey = "message-history";

    /// <summary>
    /// Appends a new <see cref="MessageHistoryEntry"/> to the envelope's metadata history chain.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The envelope whose metadata will be updated in place.</param>
    /// <param name="activityName">The name of the processing step.</param>
    /// <param name="status">The outcome of the step.</param>
    /// <param name="detail">Optional detail about the step.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="envelope"/> or <paramref name="activityName"/> is null.
    /// </exception>
    public static void AppendHistory<T>(
        IntegrationEnvelope<T> envelope,
        string activityName,
        MessageHistoryStatus status,
        string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentException.ThrowIfNullOrWhiteSpace(activityName);

        var entry = new MessageHistoryEntry(
            activityName,
            DateTimeOffset.UtcNow,
            status,
            detail);

        var history = GetHistory(envelope);
        var list = new List<MessageHistoryEntry>(history) { entry };

        envelope.Metadata[MetadataKey] = JsonSerializer.Serialize(list);
    }

    /// <summary>
    /// Reads the message history chain from the envelope's metadata.
    /// Returns an empty list if no history exists.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The envelope to read history from.</param>
    /// <returns>The ordered list of history entries.</returns>
    public static IReadOnlyList<MessageHistoryEntry> GetHistory<T>(IntegrationEnvelope<T> envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!envelope.Metadata.TryGetValue(MetadataKey, out var json) ||
            string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<MessageHistoryEntry>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
