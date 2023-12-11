using GraphQL.Language.AST;
using Sig.App.Backend.DbModel.Entities.Projects;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Sig.App.Backend.DbModel.Entities.Markets
{
    public class Market : IHaveLongIdentifier
    {
        private const int keySize = 64;
        private const int iterations = 350000;
        private HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA512;

        public long Id { get; set; }
        public string Name { get; set; }

        public IList<ProjectMarket> Projects { get; set; }

        public bool IsArchived { get; set; }

        public string RefundTransactionPassword { get; set; }
        public byte[] RefundTransactionPasswordSalt { get; set; }

        public void SetRefundTransactionPassword(string password)
        {
            RefundTransactionPassword = HashPasword(password, out var salt);
            RefundTransactionPasswordSalt = salt;
        }

        public bool VerifyPassword(string password)
        {
            if (RefundTransactionPassword != null)
            {
                return VerifyPassword(password, RefundTransactionPassword, RefundTransactionPasswordSalt);
            }
            return true;
        }

        private bool VerifyPassword(string password, string hash, byte[] salt)
        {
            var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, keySize);
            return CryptographicOperations.FixedTimeEquals(hashToCompare, Convert.FromHexString(hash));
        }

        private string HashPasword(string password, out byte[] salt)
        {
            salt = RandomNumberGenerator.GetBytes(keySize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                iterations,
                hashAlgorithm,
                keySize);
            return Convert.ToHexString(hash);
        }
    }
}
