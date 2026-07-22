namespace Sig.App.Backend.Configuration
{
    public class AxiomOptions
    {
        public bool Enabled { get; set; }
        public string DatasetName { get; set; } = "";
        public string ApiToken { get; set; } = "";
        public string Domain { get; set; } = "api.axiom.co";
        public string Environment { get; set; } = "";
    }
}
