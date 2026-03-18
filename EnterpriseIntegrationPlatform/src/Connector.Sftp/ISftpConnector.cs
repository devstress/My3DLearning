using EnterpriseIntegrationPlatform.Contracts;

namespace EnterpriseIntegrationPlatform.Connector.Sftp;

/// <summary>
/// Transfers files to and from an SFTP server, wrapping payloads in platform envelopes.
/// </summary>
public interface ISftpConnector
{
    /// <summary>
    /// Serializes the envelope payload and uploads it to the SFTP server under
    /// <c>RootPath/fileName</c>. A JSON metadata sidecar is written alongside the data file.
    /// </summary>
    /// <typeparam name="T">Payload type.</typeparam>
    /// <param name="envelope">The message envelope whose payload will be uploaded.</param>
    /// <param name="fileName">Target filename on the remote server.</param>
    /// <param name="serializer">Function that converts the payload to bytes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The full remote path of the uploaded file.</returns>
    Task<string> UploadAsync<T>(
        IntegrationEnvelope<T> envelope,
        string fileName,
        Func<T, byte[]> serializer,
        CancellationToken ct);

    /// <summary>Downloads the raw bytes of the file at the specified remote path.</summary>
    /// <param name="remotePath">Full remote path of the file to download.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File contents as a byte array.</returns>
    Task<byte[]> DownloadAsync(string remotePath, CancellationToken ct);

    /// <summary>Lists the full remote paths of all files in the specified directory.</summary>
    /// <param name="remotePath">Remote directory to list.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of full remote file paths.</returns>
    Task<IReadOnlyList<string>> ListFilesAsync(string remotePath, CancellationToken ct);
}
