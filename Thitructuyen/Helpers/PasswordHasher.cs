using System.Security.Cryptography;

namespace Thitructuyen.Helpers
{
    // Băm mật khẩu bằng PBKDF2 (không cần thư viện ngoài).
    // Định dạng lưu: {saltBase64}.{hashBase64}
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static string Hash(string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string? stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;

            // Hỗ trợ dữ liệu cũ chưa băm (so sánh thẳng) để không vỡ tài khoản seed thủ công.
            var parts = stored.Split('.', 2);
            if (parts.Length != 2)
                return stored == password;

            try
            {
                var salt = Convert.FromBase64String(parts[0]);
                var key = Convert.FromBase64String(parts[1]);

                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
                var test = pbkdf2.GetBytes(KeySize);

                return CryptographicOperations.FixedTimeEquals(test, key);
            }
            catch
            {
                return false;
            }
        }
    }
}
