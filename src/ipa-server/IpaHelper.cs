using Claunia.PropertyList;
using IpaHosting.Controllers;
using PNGDecrush;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace IpaHosting;

internal static partial class IpaHelper
{
    internal static byte[]? GetAppIcon(ZipArchive zip)
    {
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

    internal static void AssertIsAlphaNumeric(string id)
    {
        if (!AlphaNumericRegex().IsMatch(id))
        {
            throw new ArgumentException("parameter must be alpha-numeric", nameof(id));
        }
    }

    [GeneratedRegex("^[a-zA-Z0-9]+$")]
    internal static partial Regex AlphaNumericRegex();

    internal static async Task<IphoneInstallPackageInfo> GetInfoAsync(PackageKind ipa, Sha256 id)
    {
        

        var storage = Program.Services.GetRequiredService<IStorageService>();
        var ipaFileEntry = storage.GetFile(ipa, id);
        using (var stream = ipaFileEntry.FileInfo.OpenRead())
        {
            return GetInfo(stream, ipaFileEntry.MetaData.Sha256);
        }
    }

    internal static IphoneInstallPackageInfo GetInfo(Stream stream, string hash)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        return GetInfo(zip, hash);
    }

    internal static IphoneInstallPackageInfo GetInfo(ZipArchive zip, string hash)
    {
        var infoPlistEntry = zip.Entries.First(x => Regex.IsMatch(x.FullName, "Payload/[^/]+[.]app/Info[.]plist$"));

        var infoPlist = (NSDictionary)PropertyListParser.Parse(infoPlistEntry.Open());

        var infoPlistJson = ConvertInfoPlistToJson(infoPlist);

        string? identifier = infoPlist["CFBundleIdentifier"].ToString();
        string? version = infoPlist["CFBundleVersion"].ToString();
        return new IphoneInstallPackageInfo
        {
            PlistUrl = $"{Program.BaseAddress}/{MyRoutes.download}/{hash}.plist",
            IpaDownloadUrl = $"{Program.BaseAddress}/{MyRoutes.download}/{hash}.ipa",
            CFBundleIdentifier = identifier,
            CFBundleVersion = version,
            CFBundleShortVersionString = infoPlist["CFBundleShortVersionString"].ToString(),
            Sha256 = hash,
            InfoPlistXml = infoPlist.ToXmlPropertyList(),
            InfoPlistJson = infoPlistJson,
            DisplayImageUrl = new Uri($"{Program.BaseAddress}/{MyRoutes.download}/{hash}.display-image.png"),
            StorageKey = $"ipa/{identifier}/{version}/{hash}.ipa",
        };
    }

    private static string ConvertInfoPlistToJson(NSDictionary infoPlist)
    {
        var dict = infoPlist.ToDictionary();
        var opts = new JsonSerializerOptions()
        {
            WriteIndented = true,
        };
        opts.Converters.Add(new NsValueConverter());
        var res = JsonSerializer.Serialize(infoPlist, opts);
        return res;
    }

    private class NsValueConverter : JsonConverter<NSObject>
    {
        public override NSObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, NSObject value, JsonSerializerOptions options)
        {
            if (value is NSString str)
            {
                writer.WriteStringValue(str.Content);
            }
            else if (value is NSNumber num)
            {
                if (num.isBoolean())
                {
                    writer.WriteBooleanValue(num.ToBool());
                }
                else if (num.isInteger())
                {
                    writer.WriteNumberValue(num.ToLong());
                }
                else
                {
                    Debugger.Break();
                }
            }
            else if (value is NSArray array)
            {
                writer.WriteStartArray();
                foreach (var item in array)
                {
                    Write(writer, item, options);
                }
                writer.WriteEndArray();
            }
            else if (value is NSDictionary dict)
            {
                writer.WriteStartObject();
                foreach (var item in dict)
                {
                    writer.WritePropertyName(item.Key);
                    Write(writer, item.Value, options);
                }
                writer.WriteEndObject();
            }
            else
            {
                Debugger.Break();
            }
        }
    }
}

public class IphoneInstallPackageInfo
{
    public required string PlistUrl { get; init; }
    public required string IpaDownloadUrl { get; init; }
    public required string CFBundleIdentifier { get; init; }
    public required string CFBundleVersion { get; init; }
    public required string CFBundleShortVersionString { get; init; }
    public required string Sha256 { get; init; }
    public required string InfoPlistXml { get; init; }
    public required Uri DisplayImageUrl { get; init; }
    public required string StorageKey { get; init; }
    /// <summary>
    /// only for convenience, cause the NSDictionary xml is extremely verbose
    /// </summary>
    public required string InfoPlistJson { get; init; }
}

public sealed record Sha256
{
    public string Value { get; }

    public Sha256(string value)
    {
        if (!IpaHelper.AlphaNumericRegex().IsMatch(value))
        {
            throw new ArgumentException("parameter must be alpha-numeric", nameof(value));
        }

        if (value.Length != 64)
        {
            throw new ArgumentException("parameter must be 64 chars long", nameof(value));
        }

        Value = value;
    }

    public static implicit operator Sha256(string value) => new(value);
}