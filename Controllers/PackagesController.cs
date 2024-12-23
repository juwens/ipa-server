using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IpaHosting.Controllers;
[Route("api/[controller]")]
[ApiController]
public class PackagesController : ControllerBase
{
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

            string hash;
            using (var tmpFileStream = System.IO.File.OpenRead(tmpFile))
            {
                hash = await HashHelper.ComputeSHA256HashAsync(tmpFileStream);
            }

            var storagePath = PathHelper.GetAbsoluteIpaStoragePath(hash);
            if (System.IO.File.Exists(storagePath))
            {
                return Content(JsonSerializer.Serialize(new
                {
                    status = "duplicate"
                }));
            }

            System.IO.File.Move(tmpFile, storagePath);
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
