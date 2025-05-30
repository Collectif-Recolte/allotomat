using System.Collections.Generic;
using System.Linq;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Services.Mailer
{
    public class EmailBlacklistCache
    {
        private static readonly object initLock = new object();

        private HashSet<string> emails;

        public void EnsureInitialized(AppDbContext db)
        {
            if (emails != null) return;

            lock (initLock)
            {
                if (emails != null) return;

                var existingBlacklistedEmails = db.BlacklistedEmails.Select(x => x.Email).ToList();
                emails = new HashSet<string>(existingBlacklistedEmails.Select(e => e.NormalizeEmailAddress()));
            }
        }
        
        public void Add(string email)
        {
            emails.Add(email.NormalizeEmailAddress());
        }

        public bool Check(string email)
        {
            return emails.Contains(email.NormalizeEmailAddress());
        }
    }
}