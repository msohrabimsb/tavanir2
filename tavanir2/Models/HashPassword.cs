using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Security.Cryptography;

namespace tavanir2.Models
{
    public class HashingPassword
    {
        public HashedPassword HashPassword(string password, byte[] salt = null)
        {
            if (salt == null)
            {
                // generate a 128-bit salt using a secure PRNG
                salt = new byte[150];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }
            }

            byte[] hashed = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 150);

            HashedPassword res = new HashedPassword()
            {
                PasswordHash = hashed,
                PasswordSalt = salt
            };

            return res;
        }

        public bool VerifyPassword(byte[] passwordHash, byte[] passwordSalt, string passwordToCheck)
        {
            if (passwordHash == null || passwordSalt == null)
                return false;
            string hashedSavePassword = Convert.ToBase64String(passwordHash);

            // hash the given password
            string hashOfpasswordToCheck = Convert.ToBase64String(HashPassword(passwordToCheck, passwordSalt).PasswordHash);
            // compare both hashes
            if (Equals(hashedSavePassword, hashOfpasswordToCheck))
            {
                return true;
            }
            return false;
        }
    }
}
