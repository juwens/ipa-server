
using Blobject.Core;
using Blobject.Disk;
using Claunia.PropertyList;
using IpaHosting.Controllers;
using PNGDecrush;
using System.IO;
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

    internal static IphoneInstallPackageInfos GetInfo(Stream stream, string hash)
    {
        var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var infoPlistEntry = zip.Entries.First(x => Regex.IsMatch(x.FullName, "Payload/[^/]+[.]app/Info[.]plist$"));

        var infoPlist = (NSDictionary)PropertyListParser.Parse(infoPlistEntry.Open());

        string? identifier = infoPlist["CFBundleIdentifier"].ToString();
        string? version = infoPlist["CFBundleVersion"].ToString();
        return new IphoneInstallPackageInfos
        {
            PlistUrl = $"{Program.BaseAddress}/{MyRoutes.download}/{hash}.plist",
            IpaDownloadUrl = $"{Program.BaseAddress}/{MyRoutes.download}/{hash}.ipa",
            CFBundleIdentifier = identifier,
            CFBundleVersion = version,
            CFBundleShortVersionString = infoPlist["CFBundleShortVersionString"].ToString(),
            Sha256 = hash,
            FileSize = stream.Length,
            InfoPlistXml = infoPlist.ToXmlPropertyList(),
            DisplayImageUrl = new Uri($"{Program.BaseAddress}/{MyRoutes.download}/{hash}.display-image.png"),
            StorageKey = $"ipa/{identifier}/{version}/{hash}.ipa"
        };
    }

    internal static async Task<IphoneInstallPackageInfos> GetInfoAsync(Blobject.Core.BlobMetadata blob)
    {
        var cl = Program.Services.GetRequiredService<BlobClientBase>();
        var hash = Path.GetFileNameWithoutExtension(blob.Key);
        using (var blobData = await cl.GetStreamAsync(blob.Key))
        {
            return GetInfo(blobData.Data, hash);
        }
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
        public required string StorageKey { get; init; }
    }

    [GeneratedRegex("^[a-zA-Z0-9]+$")]
    private static partial Regex AlphaNumericRegex();

    internal static async Task<IphoneInstallPackageInfos> GetInfoAsync(PackageKind ipa, string id)
    {
        AssertIsAlphaNumeric(id);

        var cl = Program.Services.GetRequiredService<BlobClientBase>();
        var blob = cl.Enumerate(new EnumerationFilter() { Suffix = $"{id}.{PackageKind.Ipa}" }).Single();
        return await GetInfoAsync(blob);
    }
}
