using System.Text;
using System.Security.Cryptography;

namespace Astrion.Network
{
    /// SHA-256 hex digest of the user's password, computed on the client
    /// before the LOGIN packet leaves the box. Two upsides:
    ///
    ///   1. The wire never sees the plaintext password — TLS already
    ///      encrypts in transit, but a TLS misconfig or a future
    ///      protocol downgrade can no longer leak the cleartext.
    ///   2. Cached SessionCredentials in memory only ever holds the
    ///      hash, not the plaintext, so a process inspector / crash
    ///      dump scraper sees a digest rather than the real secret.
    ///
    /// Server-side, GamePacketHandler.handleLogin now takes the digest
    /// as-is and compares it directly against the stored Redis value.
    /// Existing accounts keep working because the server used to compute
    /// the *same* hash before storing, so the on-disk format is unchanged.
    public static class PasswordHasher
    {
        public static string Sha256Hex(string s)
        {
            if (s == null) s = "";
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
