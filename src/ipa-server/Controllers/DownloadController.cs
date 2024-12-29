﻿using Microsoft.AspNetCore.Mvc;

namespace IpaHosting.Controllers;

[Route(MyRoutes.download)]
public class DownloadController : Controller
{
    public DownloadController()
    {
    }

    [HttpGet("{id:regex([[a-z0-9]]+)}.ipa")]
    [ProducesResponseType<byte[]>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIpaAsync(string id)
    {
        var info = await IpaHelper.GetInfoAsync(PackageKind.Ipa, id);
        string fileDownloadName = $"{info.CFBundleIdentifier}.{info.CFBundleShortVersionString}.ipa";
        throw new NotImplementedException();
        //return base.File((await _blobs.GetStreamAsync(info.StorageKey)).Data, "application/octet-stream", fileDownloadName: fileDownloadName);
    }

    /// <summary>
    /// Requested via <code>itms-services://?action=download-manifest&url=https://mein.webserver.de/manifest.plist</code>
    /// 
    /// <see cref="https://support.apple.com/en-mz/guide/deployment/depce7cefc4d/web"/>
    /// <seealso cref="https://support.pressmatrix.com/hc/en-us/articles/208640329-How-do-I-distribute-my-app-via-In-House-Apple-Enterprise-Distribution-OTA"/>
    /// </summary>
    [HttpGet("{id:regex(^[[a-z0-9]]+$)}.plist")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlistAsync(string id)
    {
        var xml = await     GetManifestAsync(id);
        return base.Content(xml, "text/xml");
    }

    [HttpGet("{id:regex(^[[a-z0-9]]+)}.display-image.png")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetDisplayImage(string id)
    {
        var img = IpaHelper.GetAppIcon(id);
        return base.File(img ?? _displayImageFallback, "image/png", enableRangeProcessing: true);
    }

    private async Task<string> GetManifestAsync(string id)
    {
        var info = await IpaHelper.GetInfoAsync(PackageKind.Ipa, id);

        // https://support.pressmatrix.com/hc/en-us/articles/208640329-How-do-I-distribute-my-app-via-In-House-Apple-Enterprise-Distribution-OTA

        var title = info.CFBundleIdentifier;

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
                  <dict>
                    <key>kind</key>
                    <string>display-image</string>
                    <key>needs-shine</key>
                    <false/>
                    <key>url</key>
                    <string>{info.DisplayImageUrl}</string>
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

    private static readonly byte[] _displayImageFallback =
    {
        0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A,0x00,0x00,0x00,0x0D,0x49,0x48,0x44,0x52,0x00,0x00,0x00,0x78,0x00,0x00,0x00,0x78,0x08,0x02,0x00,0x00,0x00,0xB6,0x06,0xA1,0x85,0x00,0x00,0x00,0x01,0x73,0x52,0x47,0x42,0x00,0xAE,0xCE,0x1C,0xE9,0x00,0x00,0x00,0x04,0x67,0x41,0x4D,0x41,0x00,0x00,0xB1,0x8F,0x0B,0xFC,0x61,0x05,0x00,0x00,0x00,0x09,0x70,0x48,0x59,0x73,0x00,0x00,0x21,0x37,0x00,0x00,0x21,0x37,0x01,0x33,0x58,0x9F,0x7A,0x00,0x00,0x01,0x0D,0x49,0x44,0x41,0x54,0x78,0x5E,0xED,0xD0,0x41,0x0D,0x00,0x30,0x08,0x00,0xB1,0xD9,0xC5,0x1F,0xFE,0x98,0x8B,0x7B,0x35,0xA9,0x82,0xBE,0x9B,0x25,0x20,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x22,0x3A,0x31,0xFB,0x01,0x45,0xF1,0xAD,0xEB,0x09,0x88,0x8C,0x56,0x00,0x00,0x00,0x00,0x49,0x45,0x4E,0x44,0xAE,0x42,0x60,0x82
    };
}
