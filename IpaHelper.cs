
using Claunia.PropertyList;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace IpaHosting;

internal static class IpaHelper
{
    internal static IphoneInstallPackageInfos GetInfo(string path)
    {
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
            InfoPlistXml = infoPlist.ToXmlPropertyList()
        };
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
    }
}
