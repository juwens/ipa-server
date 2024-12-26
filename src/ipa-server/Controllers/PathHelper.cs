
namespace IpaHosting.Controllers;

internal class PathHelper
{
    public const string FileExtension = "sha256";

    internal static string GetAbsoluteIpaStoragePath(string id)
    {
        IpaHelper.AssertIsAlphaNumeric(id);

        return Path.Combine(Program.StorageDir, $"{id}.{FileExtension}");
    }


    internal static string[] GetFiles()
    {
        return Directory.GetFiles(Program.StorageDir, $"*.{FileExtension}");
    }

}