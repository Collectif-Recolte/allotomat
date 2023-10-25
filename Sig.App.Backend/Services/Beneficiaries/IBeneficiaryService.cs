using System.Threading.Tasks;

namespace Sig.App.Backend.Services.Beneficiaries
{
    public interface IBeneficiaryService
    {
        Task<bool> CurrentUserCanSeeAllBeneficiaryInfo();
    }
}