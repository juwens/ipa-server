using System.Security.Cryptography;

namespace IpaHosting;

internal static class HashHelper
{
    public static async Task<string> ComputeSHA256HashAsync(Stream stream)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = await sha256.ComputeHashAsync(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
