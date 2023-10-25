using Sig.App.Backend.DbModel.Entities.Markets;

namespace Sig.App.Backend.DbModel.Entities.Projects
{
    public class ProjectMarket
    {
        public long ProjectId { get; set; }
        public long MarketId { get; set; }

        public Project Project { get; set; }
        public Market Market { get; set; }
    }
}
