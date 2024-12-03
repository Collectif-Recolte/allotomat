using Sig.App.Backend.Services.Mailer;
using System;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class DeleteBeneficiaryEmail : EmailModel
    {
        public string FirstName { get; set; }
        public DateTime DeletionDate { get; set; }

        public override string Subject => "Votre compte a été supprimé / Your Account Has Been Deleted";

        public DeleteBeneficiaryEmail(string to, string firstName, DateTime deletionDate) : base(to)
        {
            FirstName = firstName;
            DeletionDate = deletionDate;
        }
    }
}