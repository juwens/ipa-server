
using Claunia.PropertyList;
using IpaHosting.Controllers;
using PNGDecrush;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace IpaHosting;

internal static partial class IpaHelper
{
    internal static byte[]? GetAppIcon(string id)
    {
        AssertIsAlphaNumeric(id);

        var path = PathHelper.GetAbsoluteIpaStoragePath(id);
        using var stream = File.OpenRead(path);
        var zip = new ZipArchive(stream, ZipArchiveMode.Read);

        // AppIcon60x60@2x.png
        var appIconEntry = zip.Entries.FirstOrDefault(x => Regex.IsMatch(x.FullName, "Payload/[^/]+[.]app/AppIcon60x60@2x[.]png$", RegexOptions.IgnoreCase));

        if (appIconEntry is null)
        {
            return null;
        }

        using var entryStream = appIconEntry.Open();

        
        return SanitizePng(entryStream);
    }

    /// <summary>
    /// AppIcon images are "incorrectly" encoded, in a proprietary apple png format: https://theapplewiki.com/wiki/PNG_CgBI_Format
    /// https://www.nayuki.io/page/png-file-chunk-inspector
    /// </summary>
    private static byte[] SanitizePng(Stream entryStream)
    {
        using var applePngStream = new MemoryStream();
        entryStream.CopyTo(applePngStream);
        applePngStream.Position = 0;
        
        using var sanitizedStream = new MemoryStream();
        PNGDecrusher.Decrush(applePngStream, sanitizedStream);

        sanitizedStream.Position = 0;
        return sanitizedStream.ToArray();
    }

    internal static IphoneInstallPackageInfos GetInfo(string id)
    {
        AssertIsAlphaNumeric(id);

        var path = PathHelper.GetAbsoluteIpaStoragePath(id);
        using var stream = File.OpenRead(path);
        var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var infoPlistEntry = zip.Entries.First(x => Regex.IsMatch(x.FullName, "Payload/[^/]+[.]app/Info[.]plist$"));

        var infoPlist = (NSDictionary)PropertyListParser.Parse(infoPlistEntry.Open());

        var hash = Path.GetFileNameWithoutExtension(path);
        return new IphoneInstallPackageInfos
        {
            PlistUrl = $"{Program.BaseAddress}/{MyRoutes.download}/{hash}.plist",
            IpaDownloadUrl = $"{Program.BaseAddress}/{MyRoutes.download}/{hash}.ipa",
            CFBundleIdentifier = infoPlist["CFBundleIdentifier"].ToString(),
            CFBundleVersion = infoPlist["CFBundleVersion"].ToString(),
            CFBundleShortVersionString = infoPlist["CFBundleShortVersionString"].ToString(),
            Sha256 = hash,
            FileSize = new FileInfo(path).Length,
            InfoPlistXml = infoPlist.ToXmlPropertyList(),
            DisplayImageUrl = new Uri($"{Program.BaseAddress}/{MyRoutes.download}/{hash}.display-image.png"),
            PhysicalFilePath = path,
        };
    }

    internal static void AssertIsAlphaNumeric(string id)
    {
        if (!AlphaNumericRegex().IsMatch(id))
        {
            throw new ArgumentException("parameter must be alpha-numeric", nameof(id));
        }
    }

    public class IphoneInstallPackageInfos
    {
        public required string PlistUrl { get; init; }
        public required string IpaDownloadUrl { get; init; }
        public required string CFBundleIdentifier { get; init; }
        public required string CFBundleVersion { get; init; }
        public required string CFBundleShortVersionString { get; init; }
        public required string Sha256 { get; init; }
        public required long FileSize { get; init; }
        public required string InfoPlistXml { get; init; }
        public required Uri DisplayImageUrl { get; init; }
        public required string PhysicalFilePath { get; init; }
    }

    [GeneratedRegex("^[a-zA-Z0-9]+$")]
    private static partial Regex AlphaNumericRegex();
}
