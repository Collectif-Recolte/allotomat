using GraphQL.Conventions;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class BaseProjectGraphType
    {
        private readonly Project project;

        public Id Id => project.GetIdentifier();
        public NonNull<string> Name => project.Name;
        public string Url => project.Url;
        public string CardImageFileId => project.CardImageFileId;
        public bool AllowOrganizationsAssignCards => project.AllowOrganizationsAssignCards;
        public bool BeneficiariesAreAnonymous => project.BeneficiariesAreAnonymous;
        public bool AdministrationSubscriptionsOffPlatform => project.AdministrationSubscriptionsOffPlatform;

        public BaseProjectGraphType(Project project)
        {
            this.project = project;
        }
    }
}
