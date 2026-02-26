namespace Sig.App.Backend.Constants
{
    public static class SearchCollation
    {
        /// <summary>
        /// SQL Server collation: case-insensitive (CI), accent-insensitive (AI).
        /// Use with EF.Functions.Collate(column, SearchCollation.AccentInsensitive).Contains(searchTerm).
        /// </summary>
        public const string AccentInsensitive = "SQL_Latin1_General_CP1_CI_AI";
    }
}
