namespace Terranes.Contracts.Enums;

/// <summary>
/// Supported 3D model file formats for upload and processing.
/// </summary>
public enum ModelFormat
{
    /// <summary>GL Transmission Format (glTF 2.0).</summary>
    Gltf,

    /// <summary>GL Transmission Format Binary (glTF 2.0).</summary>
    Glb,

    /// <summary>Wavefront OBJ format.</summary>
    Obj,

    /// <summary>Autodesk FBX format.</summary>
    Fbx,

    /// <summary>Universal Scene Description format.</summary>
    Usd
}
