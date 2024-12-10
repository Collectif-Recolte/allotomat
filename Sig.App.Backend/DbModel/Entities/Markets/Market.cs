using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Markets
{
    public class Market : IHaveLongIdentifier
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public IList<ProjectMarket> Projects { get; set; }
        public IList<OrganizationMarket> Organizations { get; set; }
        public IList<MarketGroupMarket> MarketGroups { get; set; }
        public IList<CashRegister> CashRegisters { get; set; }

        public bool IsArchived { get; set; }
    }
}
