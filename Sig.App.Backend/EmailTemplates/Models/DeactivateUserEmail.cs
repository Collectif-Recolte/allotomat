using Sig.App.Backend.Services.Mailer;
using System;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class DeactivateUserEmail : EmailModel
    {
        public string FirstName { get; set; }
        public DateTime DeletionDate { get; set; }

        public override string Subject => "Important : Votre compte sera supprimé dans 4 semaines / Important: Your account will be deleted in 4 weeks";

        public DeactivateUserEmail(string to, string firstName, DateTime deletionDate) : base(to)
        {
            FirstName = firstName;
            DeletionDate = deletionDate;
        }
    }
}