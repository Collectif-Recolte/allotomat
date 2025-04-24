using System;
using System.Threading.Tasks;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;

namespace Sig.App.Backend.Services.Mailer
{
    public class EmailBlacklistService : IEmailBlacklistService
    {
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly EmailBlacklistCache cache;

        public EmailBlacklistService(AppDbContext db, IClock clock, EmailBlacklistCache cache)
        {
            this.db = db;
            this.clock = clock;
            this.cache = cache;
        }

        public async Task AddToBlacklist(string email, string reason, string emailSource, DateTime emailSentAt, string emailSubject)
        {
            var blacklistItem = await db.BlacklistedEmails.FindAsync(email);
            
            if (blacklistItem == null)
            {
                blacklistItem = new BlacklistedEmail {
                    Email = email,
                    FirstAddedAt = clock.GetCurrentInstant().ToDateTimeUtc()
                };

                db.BlacklistedEmails.Add(blacklistItem);
            }

            blacklistItem.LastAddedAt = clock.GetCurrentInstant().ToDateTimeUtc();
            blacklistItem.AddedCount++;
            blacklistItem.LastReason = reason;
            blacklistItem.EmailSource = emailSource;
            blacklistItem.EmailSentAt = emailSentAt;
            blacklistItem.EmailSubject = emailSubject;

            await db.SaveChangesAsync();
            
            cache.EnsureInitialized(db);
            cache.Add(email);
        }

        public bool IsBlacklisted(string email)
        {
            cache.EnsureInitialized(db);
            return cache.Check(email);
        }
    }
}