namespace EnterpriseIntegrationPlatform.Connectors.Sftp;

/// <summary>
/// Transfers files over SFTP (SSH File Transfer Protocol).
/// </summary>
public interface ISftpConnector
{
    /// <summary>Uploads a file to the remote SFTP server.</summary>
    Task UploadAsync(
        string remotePath,
        byte[] content,
        CancellationToken ct = default);

    /// <summary>Downloads a file from the remote SFTP server.</summary>
    Task<byte[]> DownloadAsync(
        string remotePath,
        CancellationToken ct = default);

    /// <summary>Lists files in a remote directory.</summary>
    Task<IReadOnlyList<string>> ListAsync(
        string remoteDirectory,
        CancellationToken ct = default);

    /// <summary>Deletes a file on the remote SFTP server.</summary>
    Task DeleteAsync(string remotePath, CancellationToken ct = default);
}
