using System;
using System.Threading.Tasks;

namespace Sig.App.Backend.Services.Mailer
{
    public interface IEmailBlacklistService
    {
        Task AddToBlacklist(string email, string reason, string emailSource, DateTime emailSentAt, string emailSubject);
        bool IsBlacklisted(string email);
    }
}