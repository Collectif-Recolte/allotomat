using Sig.App.Backend.DbModel.Entities.Cards;

namespace Sig.App.Backend.Requests.Queries.Cards
{
    public class CardBalanceReport
    {
        public Card Card { get; set; }
        public decimal Total { get; set; }
    }
}
