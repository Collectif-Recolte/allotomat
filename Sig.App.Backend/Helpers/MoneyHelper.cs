namespace Sig.App.Backend.Helpers
{
    public static class MoneyHelper
    {
        public const string FR = "fr-CA";
        public const string EN = "en-US";

        public static string GetMoneyFormat(this decimal amount, string lang)
        {
            var amountString = amount.ToString("##0.00");
            if (lang == FR)
            {
                return $"{amountString.Replace(".", ",")} $".Replace(".", ",");
            }
            else
            {
                return $"${amountString.Replace(",", ".")}".Replace(",", ".");
            }
        }
    }
}
