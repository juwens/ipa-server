using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IpaHosting.Controllers;
[Route("api/[controller]")]
[ApiController]
public class PackagesController : ControllerBase
{
    private readonly IStorageService _storage;

    public PackagesController(IStorageService storage)
    {
        _storage = storage;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UploadAsync(string token)
    {
        if (token != Program.Token)
        {
            return Unauthorized();
        }

        var uploadFileStream = Request.Body;

        if (Request.Headers.ContentLength > Program.UploadMaxLength)
        {
            return BadRequest("too big");
        }

        var tmpFile = Path.GetTempFileName();
        try
        {
            using (var tmpFileStream = System.IO.File.OpenWrite(tmpFile))
            {
                await Request.Body.CopyToAsync(tmpFileStream);
            }

            await _storage.SaveAsync(PackageKind.Ipa, tmpFile);
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            if (System.IO.File.Exists(tmpFile))
            {
                System.IO.File.Delete(tmpFile);
            }
        }

        return Content(JsonSerializer.Serialize(new
        {
            status = "created"
        }));
    }
}
