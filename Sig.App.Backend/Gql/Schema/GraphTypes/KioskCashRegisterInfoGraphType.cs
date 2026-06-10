namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class KioskCashRegisterInfoGraphType
    {
        public bool IsValid { get; set; }
        public string CashRegisterName { get; set; }
        public bool MarketIsDisabled { get; set; }
    }
}
