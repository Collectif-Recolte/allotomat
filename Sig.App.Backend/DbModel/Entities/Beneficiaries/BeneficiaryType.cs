using Sig.App.Backend.DbModel.Entities.Projects;
using System.Collections.Generic;
using System.Linq;

namespace Sig.App.Backend.DbModel.Entities.Beneficiaries
{
    public class BeneficiaryType : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public string Name { get; set; }
        public string Keys { get; set; }

        public long ProjectId { get; set; }
        public Project Project { get; set; }

        public IList<Beneficiary> Beneficiaries { get; set; }
        
        public string[] GetKeys()
        {
            return Keys.Split(";");
        }

        public void SetKeys(string[] keys)
        {
            Keys = string.Join(";", keys.Distinct().Select(x => x.Trim().ToLower()));
        }
    }
}
