using GraphQL.Conventions.Attributes;
using GraphQL.Conventions.Types.Descriptors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Threading.Tasks;
using Sig.App.Backend.Authorization.Requirements;

namespace Sig.App.Backend.Authorization
{
    public class AnnotatePolicyAttribute : MetaDataAttributeBase
    {
        public static IAuthorizationPolicyProvider AuthorizationPolicyProvider { get; set; }

        private readonly string policy;

        public AnnotatePolicyAttribute(string policy)
        {
            this.policy = policy;
        }

        public override void DeriveMetaData(GraphEntityInfo entity)
        {
            base.DeriveMetaData(entity);

            if (string.IsNullOrWhiteSpace(entity.Description))
                entity.Description = "";
            else
                entity.Description += "\n\n";

            entity.Description += GetPolicyDescription(policy).GetAwaiter().GetResult();
        }

        public static async Task<string> GetPolicyDescription(string name)
        {
            var desc = $"**Authorization policy:** `{name}`";

            var policy = await AuthorizationPolicyProvider.GetPolicyAsync(name);
            if (policy != null)
            {
                foreach (var requirement in policy.Requirements)
                {
                    switch (requirement)
                    {
                        case IDescribedRequirement dr:
                            desc += $"\n* {dr.Describe()}";
                            break;
                        case DenyAnonymousAuthorizationRequirement _:
                            desc += "\n* Requires a logged in user.";
                            break;
                        case ClaimsAuthorizationRequirement cr:
                            desc +=
                                $"\n* Current user has {cr.ClaimType.Replace("Sig.App:", "")} = {string.Join(',', cr.AllowedValues)}";
                            break;
                        default:
                            desc += $"\n* {requirement.GetType().Name}";
                            break;
                    }
                }
            }

            return desc;
        }
    }
}