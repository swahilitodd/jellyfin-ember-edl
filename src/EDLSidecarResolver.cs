using System;
using System.IO;

namespace Jellyfin.Plugin.EmberEDL;

/// <summary>Only the exact same-name .edl next to a library video is eligible.</summary>
public static class EDLSidecarResolver
{
    public const long MaxEDLBytes = 1_048_576;

    public static string? Resolve(string? mediaPath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath) || !Path.IsPathFullyQualified(mediaPath))
            return null;
        var extension = Path.GetExtension(mediaPath);
        if (string.IsNullOrWhiteSpace(extension) || extension.Equals(".edl", StringComparison.OrdinalIgnoreCase))
            return null;
        // Unlike a path-based API, this path originates from Jellyfin's library database.
        // Sidecar is tied to the media's filename and parent folder: E01.mkv -> E01.edl.
        var sidecar = Path.ChangeExtension(mediaPath, ".edl");
        return File.Exists(sidecar) ? sidecar : null;
    }
}
