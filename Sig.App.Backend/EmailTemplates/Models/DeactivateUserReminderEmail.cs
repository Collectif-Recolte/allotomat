using Sig.App.Backend.Services.Mailer;
using System;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class DeactivateUserReminderEmail : EmailModel
    {
        public string FirstName { get; set; }
        public DateTime DeletionDate { get; set; }

        public override string Subject => "Rappel : Votre compte sera supprimé dans 2 semaines / Reminder: Your Account Will Be Deleted in 2 Weeks";

        public DeactivateUserReminderEmail(string to, string firstName, DateTime deletionDate) : base(to)
        {
            FirstName = firstName;
            DeletionDate = deletionDate;
        }
    }
}