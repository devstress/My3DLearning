using Microsoft.Extensions.Options;
using Renci.SshNet;

namespace EnterpriseIntegrationPlatform.Connector.Sftp;

/// <summary>
/// Production <see cref="ISftpClient"/> implementation backed by SSH.NET's
/// <see cref="Renci.SshNet.SftpClient"/>.
/// </summary>
public sealed class SshNetSftpClient : ISftpClient, IDisposable
{
    private readonly Renci.SshNet.SftpClient _client;

    /// <summary>
    /// Initialises a new <see cref="SshNetSftpClient"/> using the supplied options.
    /// </summary>
    /// <param name="options">SFTP connection options.</param>
    public SshNetSftpClient(IOptions<SftpConnectorOptions> options)
    {
        var opts = options.Value;
        _client = new Renci.SshNet.SftpClient(opts.Host, opts.Port, opts.Username, opts.Password);
        _client.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(opts.TimeoutMs);
    }

    /// <inheritdoc />
    public bool IsConnected => _client.IsConnected;

    /// <inheritdoc />
    public void Connect() => _client.Connect();

    /// <inheritdoc />
    public void Disconnect() => _client.Disconnect();

    /// <inheritdoc />
    public void UploadFile(Stream input, string remotePath) =>
        _client.UploadFile(input, remotePath);

    /// <inheritdoc />
    public Stream DownloadFile(string remotePath)
    {
        var ms = new MemoryStream();
        _client.DownloadFile(remotePath, ms);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    /// <inheritdoc />
    public IEnumerable<string> ListFiles(string remotePath) =>
        _client.ListDirectory(remotePath)
               .Where(f => f.Name != "." && f.Name != "..")
               .Select(f => f.FullName);

    /// <inheritdoc />
    public void DeleteFile(string remotePath) => _client.DeleteFile(remotePath);

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();
}
