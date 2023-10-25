using GraphQL.Conventions;
using GraphQL.Conventions.Attributes;
using GraphQL.Conventions.Execution;
using GraphQL.Conventions.Types.Descriptors;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Authorization
{
    public class ApplyPolicyAttribute : ExecutionFilterAttributeBase, IMetaDataAttribute
    {
        private readonly string policy;

        public ApplyPolicyAttribute(string policy)
        {
            this.policy = policy;
        }

        public override async Task<object> Execute(IResolutionContext context, FieldResolutionDelegate next)
        {
            var authorizationService =
                (IAuthorizationService)context.DependencyInjector.Resolve(typeof(IAuthorizationService).GetTypeInfo());

            var authorizationResult = await authorizationService.AuthorizeAsync(
                ((IAppUserContext)context.UserContext).CurrentUser,
                context,
                policy);

            if (!authorizationResult.Succeeded)
                throw new UnauthorizedAccessException();

            return await base.Execute(context, next);
        }

        bool IMetaDataAttribute.ShouldBeApplied(GraphEntityInfo entity) => true;

        void IMetaDataAttribute.DeriveMetaData(GraphEntityInfo entity)
        {
            if (string.IsNullOrWhiteSpace(entity.Description))
                entity.Description = "";
            else
                entity.Description += "\n\n";

            entity.Description += AnnotatePolicyAttribute.GetPolicyDescription(policy).GetAwaiter().GetResult();
        }
    }
}