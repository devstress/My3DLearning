namespace EnterpriseIntegrationPlatform.Connector.FileSystem;

/// <summary>
/// Configuration options for the File connector.
/// </summary>
public sealed class FileConnectorOptions
{
    /// <summary>Root directory where files are written and read (required).</summary>
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>File encoding name used when writing text. Default is <c>utf-8</c>.</summary>
    public string Encoding { get; set; } = "utf-8";

    /// <summary>
    /// When <c>true</c>, the root directory (and any sub-directories) are created
    /// if they do not already exist. Default is <c>true</c>.
    /// </summary>
    public bool CreateDirectoryIfNotExists { get; set; } = true;

    /// <summary>
    /// When <c>false</c>, writing to a path where a file already exists throws
    /// <see cref="InvalidOperationException"/>. Default is <c>false</c>.
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Pattern used to generate filenames. Supported tokens:
    /// <c>{MessageId}</c>, <c>{MessageType}</c>, <c>{CorrelationId}</c>,
    /// <c>{Timestamp:yyyyMMddHHmmss}</c>.
    /// Default is <c>{MessageId}-{MessageType}.json</c>.
    /// </summary>
    public string FilenamePattern { get; set; } = "{MessageId}-{MessageType}.json";
}
