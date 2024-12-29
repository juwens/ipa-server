using System.Security.Cryptography;

namespace IpaHosting;

internal static class HashHelper
{
    public static async Task<(string Text, byte[] Bytes)> ComputeSHA256HashAsync(Stream stream)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = await sha256.ComputeHashAsync(stream);
            string text = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            return (text, bytes);
        }
    }
}
