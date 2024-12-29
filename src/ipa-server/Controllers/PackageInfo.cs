
namespace IpaHosting.Controllers;

public record PackageInfo
{
    public required DateTime UploadDateUtc { get; init; }
    public required Sha256 Sha256 { get; init; }
    public required long FileSize { get; init; }
}