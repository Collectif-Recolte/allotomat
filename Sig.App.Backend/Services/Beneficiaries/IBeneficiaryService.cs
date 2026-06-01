using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities.Projects;

namespace Sig.App.Backend.Services.Beneficiaries
{
    public interface IBeneficiaryService
    {
        Task<bool> CurrentUserCanSeeAllBeneficiaryInfo();
        bool ShouldAnonymizeBeneficiaries(Project? project, bool canSeeAll);
        Task<bool> ShouldAnonymizeBeneficiaries(Project? project);
    }
}