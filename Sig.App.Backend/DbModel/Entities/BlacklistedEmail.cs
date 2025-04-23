using System;

namespace Sig.App.Backend.DbModel.Entities
{
    public class BlacklistedEmail
    {
        public string Email { get; set; }
        public string LastReason { get; set; }
        public DateTime FirstAddedAt { get; set; }
        public DateTime LastAddedAt { get; set; }
        public int AddedCount { get; set; }
        public string EmailSource { get; set; }
        public DateTime EmailSentAt { get; set; }
        public string EmailSubject { get; set; }
    }
}