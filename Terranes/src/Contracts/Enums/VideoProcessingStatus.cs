namespace Terranes.Contracts.Enums;

/// <summary>
/// Processing status for AI video-to-3D conversion.
/// </summary>
public enum VideoProcessingStatus
{
    /// <summary>Video has been uploaded and is queued.</summary>
    Queued,

    /// <summary>Video is being analysed for structure extraction.</summary>
    Analysing,

    /// <summary>3D mesh is being generated from the video frames.</summary>
    GeneratingMesh,

    /// <summary>Processing completed successfully.</summary>
    Completed,

    /// <summary>Processing failed.</summary>
    Failed
}
