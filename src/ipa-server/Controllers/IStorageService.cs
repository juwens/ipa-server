using Blobject.Core;

namespace IpaHosting.Controllers;

public sealed class StorageService : IStorageService
{
    private readonly BlobClientBase _blobs;

    public StorageService(BlobClientBase blobs)
    {
        _blobs = blobs;
    }

    public IReadOnlyList<string> GetIdentifiers(PackageKind kind)
    {
        return _blobs.Enumerate(new EnumerationFilter() { Prefix = $"{kind.ToString().ToLower()}/"})
            .Select(x => x.Key.Split('/')[1])
            .Distinct()
            .ToArray();
    }

    public IEnumerable<BlobMetadata> GetPackages(PackageKind kind, string identifier)
    {
        EnumerationFilter filter = new EnumerationFilter() { Prefix = $"{kind.ToString().ToLower()}/{identifier}", Suffix = ".ipa" };
        return _blobs.Enumerate(filter);
    }

    public async Task SaveAsync(PackageKind kind, string tmpFile)
    {
        string hash;
        using (var tmpFileStream = File.OpenRead(tmpFile))
        {
            hash = await HashHelper.ComputeSHA256HashAsync(tmpFileStream);
        }

        using (var tmpFileStream = File.OpenRead(tmpFile))
        {
            var ipaInfo = IpaHelper.GetInfo(tmpFileStream, hash);

            tmpFileStream.Position = 0;
            await _blobs.WriteAsync((string?)$"{kind.ToString().ToLower()}/{ipaInfo.CFBundleIdentifier}/{ipaInfo.CFBundleVersion}/{hash}.ipa", "application/octet-stream", new FileInfo(tmpFile).Length, tmpFileStream);
        }
    }
}

public interface IStorageService
{
    Task SaveAsync(PackageKind kind, string tmpFile);
    IReadOnlyList<string> GetIdentifiers(PackageKind kind);
    IEnumerable<BlobMetadata> GetPackages(PackageKind kind, string identifier);
}

public enum PackageKind
{
    Ipa
}