using System.Threading.Tasks;

namespace Sig.App.Backend.Services.Mailer
{
    public interface IMailer
    {
        Task Send<T>(T model) where T : EmailModel;
    }
}