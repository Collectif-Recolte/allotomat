using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Projects
{
    public class Project : IHaveLongIdentifier
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string CardImageFileId { get; set; }
        public string Url { get; set; }

        public IList<ProjectMarket> Markets { get; set; }
        public IList<Organization> Organizations { get; set; }
        public IList<Subscription> Subscriptions { get; set; }
        public IList<Card> Cards { get; set; }
        public IList<BeneficiaryType> BeneficiaryTypes { get; set; }
        public IList<ProductGroup> ProductGroups { get; set; }

        public bool AllowOrganizationsAssignCards { get; set; } = false;
        public bool BeneficiariesAreAnonymous { get; set; } = false;
        public bool AdministrationSubscriptionsOffPlatform { get; set; } = false;

    }
}
