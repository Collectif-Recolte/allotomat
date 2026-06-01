using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Services.System;

namespace Sig.App.Backend.Services.Beneficiaries
{
    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly AppDbContext db;
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly UserManager<AppUser> userManager;

        public BeneficiaryService(AppDbContext db, ICurrentUserAccessor currentUserAccessor, UserManager<AppUser> userManager)
        {
            this.db = db;
            this.currentUserAccessor = currentUserAccessor;
            this.userManager = userManager;
        }

        public async Task<bool> CurrentUserCanSeeAllBeneficiaryInfo()
        {
            var beneficiariesAreAnonymous = false;
            var currentUser = await currentUserAccessor.GetCurrentUser();

            if (currentUser.Type == UserType.ProjectManager)
            {
                var existingClaims = await userManager.GetClaimsAsync(currentUser);
                var existingProjectClaims = existingClaims.Where(x => x.Type == AppClaimTypes.ProjectManagerOf).Select(x => x.Value);
                var results = await db.Projects.Where(x => existingProjectClaims.Contains(x.Id.ToString())).ToListAsync();
                beneficiariesAreAnonymous = results.FirstOrDefault()?.BeneficiariesAreAnonymous ?? true;
            }

            return !beneficiariesAreAnonymous;
        }

        public bool ShouldAnonymizeBeneficiaries(Project? project, bool canSeeAll)
        {
            if (project == null) return true;
            if (!project.BeneficiariesAreAnonymous) return false;
            return !canSeeAll;
        }

        public async Task<bool> ShouldAnonymizeBeneficiaries(Project? project)
        {
            return ShouldAnonymizeBeneficiaries(project, await CurrentUserCanSeeAllBeneficiaryInfo());
        }
    }
}