using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.EmberEDL;

[ApiController]
[Route("EmberEDL")]
[Authorize]
public sealed class EDLController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;

    public EDLController(ILibraryManager libraryManager) => _libraryManager = libraryManager;

    // Important: require a user identity and use Jellyfin's USER-SCOPED item lookup.
    // No item path, custom filename, or directory is accepted from the caller.
    [HttpGet("{itemId:guid}")]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEDL([FromRoute] Guid itemId, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();
        const string jellyfinUserClaim = "Jellyfin-UserId";
        var claimValue = User.Claims.FirstOrDefault(c => c.Type.Equals(jellyfinUserClaim, StringComparison.OrdinalIgnoreCase))?.Value;
        if (!Guid.TryParse(claimValue, out var userId) || userId == Guid.Empty)
            return Unauthorized();

        // Jellyfin enforces library/user visibility when a user id is supplied.
        var item = _libraryManager.GetItemById<BaseItem>(itemId, userId);
        if (item is not Video || string.IsNullOrWhiteSpace(item.Path))
            return NotFound();

        string? edlPath;
        try { edlPath = EDLSidecarResolver.Resolve(item.Path); }
        catch (ArgumentException) { return NotFound(); }
        catch (NotSupportedException) { return NotFound(); }
        if (edlPath is null) return NotFound();

        try
        {
            var metadata = new FileInfo(edlPath);
            if (metadata.Length > EDLSidecarResolver.MaxEDLBytes)
                return StatusCode(StatusCodes.Status413PayloadTooLarge, "EDL exceeds 1 MiB size limit");
            // Sharing permits an EDL editor to replace a file while someone is watching.
            await using var stream = new FileStream(edlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous);
            using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
            var text = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            if (Encoding.UTF8.GetByteCount(text) > EDLSidecarResolver.MaxEDLBytes)
                return StatusCode(StatusCodes.Status413PayloadTooLarge, "EDL exceeds 1 MiB size limit");
            Response.Headers.CacheControl = "no-store";
            return Content(text, "text/plain; charset=utf-8");
        }
        catch (FileNotFoundException) { return NotFound(); }
        catch (DirectoryNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException) { return StatusCode(StatusCodes.Status503ServiceUnavailable, "EDL file not readable by Jellyfin"); }
        catch (IOException) { return StatusCode(StatusCodes.Status503ServiceUnavailable, "EDL file unavailable"); }
    }
}
