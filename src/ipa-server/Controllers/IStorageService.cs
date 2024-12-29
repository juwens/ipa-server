using LiteDB;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace IpaHosting.Controllers;

internal sealed class StorageService : IStorageService
{
    private static BsonMapper _mapper = new();
    private static readonly ConcurrentDictionary<string, byte[]?> _imageCache = new();

    static StorageService()
    {
        IpaFileMetaData.Configure(_mapper);
    }

    public StorageService()
    {
    }

    public LiteDatabase OpenDb(bool readWrite = true)
    {
        var path = Path.Combine(Program.StorageDir, "ipa-server.litedb");
        var connStr = new ConnectionString()
        {
            Filename = path,
            Connection = ConnectionType.Shared,
            ReadOnly = !readWrite,
        };
        var db = new LiteDatabase(connStr);
        //db.GetCollection("_files").EnsureIndex("metadata." + nameof(IpaMetaData.CFBundleIdentifier));
        return db;
    }

    public IReadOnlyList<string> GetIdentifiers(PackageKind kind)
    {
        using var db = OpenDb();

        var mimeType = kind switch
        {
            PackageKind.Ipa => "application/x-itunes-ipa",
            PackageKind.Apk => throw new NotImplementedException(),
        };

        var ids = db.FileStorage
            .Find(x => x.MimeType == mimeType)
            .Select(x => _mapper.Deserialize<IpaFileMetaData>(x.Metadata).CFBundleIdentifier)
            .Distinct()
            .ToArray();

        return ids;
    }

    public IEnumerable<PackageInfo> GetPackages(PackageKind kind, string identifier)
    {
        using var db = OpenDb(readWrite: true);

        return db.FileStorage.FindAll()
            .Select(x => (File: x, Metadata: _mapper.Deserialize<IpaFileMetaData>(x.Metadata)))
            .Where(x =>
        {
            return x.Metadata.PackageKind == kind && x.Metadata.CFBundleIdentifier == identifier;
        })
        .Select(x =>
        {
            return new PackageInfo()
            {
                UploadDateUtc = x.File.UploadDate.ToUniversalTime(),
                Sha256 = x.Metadata.Sha256,
                FileSize = x.File.Length,
            };
        });
    }

    public async Task SaveAsync(PackageKind kind, string tmpFile)
    {
        string hashString;
        byte[] hashBytes;
        using (var tmpFileStream = File.OpenRead(tmpFile))
        {
            (hashString, hashBytes) = await HashHelper.ComputeSHA256HashAsync(tmpFileStream);
        }

        using var db = OpenDb(readWrite: true);
        using (var tmpFileStream = File.OpenRead(tmpFile))
        {
            using var zip = new ZipArchive(tmpFileStream, ZipArchiveMode.Read);


            var ipaInfo = IpaHelper.GetInfo(zip, hashString);

            var metaData = new IpaFileMetaData()
            {
                Sha256 = hashString,
                CFBundleIdentifier = ipaInfo.CFBundleIdentifier,
                CFBundleVersion = ipaInfo.CFBundleVersion,
                CFBundleShortVersionString = ipaInfo.CFBundleShortVersionString,
                InfoPlistXml = ipaInfo.InfoPlistXml,
                InfoPlistJson = ipaInfo.InfoPlistJson,
                PackageKind = kind,
            };
            var nameBase = $"{ipaInfo.CFBundleIdentifier}.{ipaInfo.CFBundleVersion}";

            tmpFileStream.Position = 0;
            db.FileStorage.Upload(hashString, $"{nameBase}.ipa", tmpFileStream, metadata: _mapper.Serialize(metaData).AsDocument);

            var appIcon = IpaHelper.GetAppIcon(zip);
            if (appIcon != null)
            {
                db.FileStorage.Upload($"{hashString}.appicon", $"{nameBase}.appicon.png", new MemoryStream(appIcon));
            }
        }
    }

    public IpaFileInfo GetFile(PackageKind ipa, Sha256 id)
    {
        var db = OpenDb();
        var file = db.FileStorage.Find(x => x.Id == id.Value).First();
        return new(file, _mapper.Deserialize<IpaFileMetaData>(file.Metadata));
    }

    public byte[]? GetDisplayImage(Sha256 id)
    {
        return _imageCache.GetOrAdd(id.Value, value =>
        {
            var db = OpenDb();
            var entry = db.FileStorage.Find(x => x.Id == value + ".appicon").FirstOrDefault();

            if (entry is null)
            {
                return null;
            }

            using var ms = new MemoryStream();
            using (var sourceStream = entry.OpenRead())
            {
                sourceStream.CopyTo(ms);
            }

            ms.Position = 0;
            return ms.ToArray();
        });
    }
}

public sealed class IpaFileMetaData
{
    public string CFBundleIdentifier { get; set; }
    public string Sha256 { get; set; }
    public string CFBundleVersion { get; set; }
    public string CFBundleShortVersionString { get; set; }
    public string InfoPlistXml { get; set; }
    public string InfoPlistJson { get; set; }
    public PackageKind PackageKind { get; set; }

    internal static void Configure(BsonMapper mapper)
    {
        mapper.Entity<IpaFileMetaData>()
            .Field(x => x.CFBundleIdentifier, nameof(IpaFileMetaData.CFBundleIdentifier))
            .Field(x => x.CFBundleVersion, nameof(IpaFileMetaData.CFBundleVersion))
            .Field(x => x.CFBundleShortVersionString, nameof(IpaFileMetaData.CFBundleShortVersionString))
            .Field(x => x.Sha256, nameof(IpaFileMetaData.Sha256))
            .Field(x => x.InfoPlistXml, nameof(IpaFileMetaData.InfoPlistXml))
            .Field(x => x.InfoPlistJson, nameof(IpaFileMetaData.InfoPlistJson))
            .Field(x => x.PackageKind, nameof(IpaFileMetaData.PackageKind));
    }
}

public interface IStorageService
{
    Task SaveAsync(PackageKind kind, string tmpFile);
    IReadOnlyList<string> GetIdentifiers(PackageKind kind);
    IEnumerable<PackageInfo> GetPackages(PackageKind kind, string identifier);
    IpaFileInfo GetFile(PackageKind ipa, Sha256 id);
    byte[]? GetDisplayImage(Sha256 id);
}

public enum PackageKind
{
    Ipa,
    Apk
}

public record struct IpaFileInfo(
    LiteFileInfo<string> FileInfo,
    IpaFileMetaData MetaData);