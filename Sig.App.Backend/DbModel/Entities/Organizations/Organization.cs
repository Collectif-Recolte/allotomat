using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Projects;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Organizations
{
    public class Organization : IHaveLongIdentifier
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public long ProjectId { get; set; }
        public Project Project { get; set; }

        public IList<Beneficiary> Beneficiaries { get; set; }
        public IList<BudgetAllowance> BudgetAllowances { get; set; }
        public IList<OrganizationMarket> Markets { get; set; }
    }
}