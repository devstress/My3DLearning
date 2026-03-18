namespace EnterpriseIntegrationPlatform.Connector.Sftp;

/// <summary>
/// Thin abstraction over an SFTP client, allowing the connector to be tested without
/// a real SSH server.
/// </summary>
public interface ISftpClient
{
    /// <summary>Opens the SSH connection and SFTP session.</summary>
    void Connect();

    /// <summary>Closes the SFTP session and SSH connection.</summary>
    void Disconnect();

    /// <summary>Uploads a stream to the specified remote path, overwriting any existing file.</summary>
    /// <param name="input">The data stream to upload.</param>
    /// <param name="remotePath">Full remote path including filename.</param>
    void UploadFile(Stream input, string remotePath);

    /// <summary>
    /// Downloads the remote file into a new <see cref="Stream"/> seeked to position 0.
    /// </summary>
    /// <param name="remotePath">Full remote path of the file to download.</param>
    /// <returns>A readable <see cref="Stream"/> containing the file contents.</returns>
    Stream DownloadFile(string remotePath);

    /// <summary>
    /// Lists the full paths of all entries in the specified remote directory,
    /// excluding <c>.</c> and <c>..</c>.
    /// </summary>
    /// <param name="remotePath">Remote directory path to list.</param>
    /// <returns>An enumerable of full remote file paths.</returns>
    IEnumerable<string> ListFiles(string remotePath);

    /// <summary>Deletes the file at the specified remote path.</summary>
    /// <param name="remotePath">Full remote path of the file to delete.</param>
    void DeleteFile(string remotePath);

    /// <summary>Gets a value indicating whether the client is currently connected.</summary>
    bool IsConnected { get; }
}
