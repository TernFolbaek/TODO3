using System;
using System.Security.Cryptography;

public static class SecurityKeyHelper
{
    public static string GenerateSecureKey()
    {
        using (var random = new RNGCryptoServiceProvider())
        {
            var keyBytes = new byte[32]; // 32 bytes = 256 bits
            random.GetBytes(keyBytes);
            return Convert.ToBase64String(keyBytes);
        }
    }
}