using Microsoft.AspNetCore.Mvc;

namespace IpaHosting.Controllers;

[Route(MyRoutes.download)]
public class DownloadController : Controller
{
    [HttpGet("{id:regex([[a-z0-9]]+)}.ipa")]
    [ProducesResponseType<byte[]>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetIpa(string id)
    {
        // GetFileName is an additional layer of security to prevent absolute or relative paths
        var file = Path.Combine(Program.PackagesDir, $"{Path.GetFileName(id)}.{Program.FileExtension}");
        var info = IpaHelper.GetInfo(file);
        string fileDownloadName = $"{info.CFBundleIdentifier}.{info.CFBundleShortVersionString}.ipa";
        return base.PhysicalFile(file, "application/octet-stream", fileDownloadName: fileDownloadName);
    }

    /// <summary>
    /// Requested via <code>itms-services://?action=download-manifest&url=https://mein.webserver.de/manifest.plist</code>
    /// 
    /// <see cref="https://support.apple.com/en-mz/guide/deployment/depce7cefc4d/web"/>
    /// <seealso cref="https://support.pressmatrix.com/hc/en-us/articles/208640329-How-do-I-distribute-my-app-via-In-House-Apple-Enterprise-Distribution-OTA"/>
    /// </summary>
    [HttpGet("{id:regex(^[[a-z0-9]]+[[.]]plist$)}")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetPlist(string id)
    {
        Response.ContentType = "text/xml";
        var xml = GetManifest(id);
        return base.Content(xml);
    }

    private string GetManifest(string id)
    {
        var info = IpaHelper.GetInfo(Path.Combine(Program.PackagesDir, $"{Path.GetFileNameWithoutExtension(id)}.{Program.FileExtension}"));

        // https://support.pressmatrix.com/hc/en-us/articles/208640329-How-do-I-distribute-my-app-via-In-House-Apple-Enterprise-Distribution-OTA

        var title = "VIS.App";

        return
        $"""
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
        <plist version="1.0">
          <dict>
            <key>items</key>
            <array>
              <dict>
                <key>assets</key>
                <array>
                  <dict>
                    <key>kind</key>
                    <string>software-package</string>
                    <key>url</key>
                    <string>{info.IpaDownloadUrl}</string>
                  </dict>
                </array>
                <key>metadata</key>
                <dict>
                  <key>bundle-identifier</key>
                  <string>{info.CFBundleIdentifier}</string>
                  <key>bundle-version</key>
                  <string>{info.CFBundleVersion}</string>
                  <key>kind</key>
                  <string>software</string>
                  <key>title</key>
                  <string>{title}</string>
                </dict>
              </dict>
            </array>
        </dict>
        </plist> 
        """;
    }
}
