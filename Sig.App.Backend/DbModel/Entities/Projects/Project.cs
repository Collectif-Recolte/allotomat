using GraphQL.Language.AST;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Sig.App.Backend.DbModel.Entities.Projects
{
    public class Project : IHaveLongIdentifier
    {
        private const int keySize = 64;
        private const int iterations = 350000;
        private HashAlgorithmName hashAlgorithm = HashAlgorithmName.SHA512;

        public long Id { get; set; }
        public string Name { get; set; }
        public string CardImageFileId { get; set; }
        public string Url { get; set; }

        public IList<ProjectMarket> Markets { get; set; }
        public IList<Organization> Organizations { get; set; }
        public IList<Subscription> Subscriptions { get; set; }
        public IList<Card> Cards { get; set; }
        public IList<BeneficiaryType> BeneficiaryTypes { get; set; }
        public IList<ProductGroup> ProductGroups { get; set; }

        public bool AllowOrganizationsAssignCards { get; set; } = false;
        public bool BeneficiariesAreAnonymous { get; set; } = false;
        public bool AdministrationSubscriptionsOffPlatform { get; set; } = false;

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
