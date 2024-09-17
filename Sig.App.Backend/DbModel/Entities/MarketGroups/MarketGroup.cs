using Sig.App.Backend.DbModel.Entities.Projects;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.MarketGroups
{
    public class MarketGroup : IHaveLongIdentifier
    {
        public long Id { get; set; }
        public long ProjectId { get; set; }
        public string Name { get; set; }

        public IList<MarketGroupMarket> Markets { get; set; }
        public Project Project { get; set; }
        public bool IsArchived { get; set; }
    }
}
