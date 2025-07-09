using Sig.App.Backend.DbModel.Enums;

namespace Sig.App.Backend.Helpers
{
    public static class CardHelper
    {
        public static string GetCardStatus(CardStatus status)
        {
            switch (status)
            {
                case CardStatus.Unassigned:
                {
                    return "Non attribué/Unassigned";
                }
                case CardStatus.Assigned:
                {
                    return "Attribué/Assigned";
                }
                case CardStatus.Deactivated:
                {
                    return "Désactivé/Deactivated";
                }
                case CardStatus.GiftCard:
                {
                    return "Carte cadeau/Gift-Card";
                }
                case CardStatus.Lost:
                {
                    return "Perdue/Lost";
                }
                default:
                {
                    return "";
                }
            }
        }
    }
}