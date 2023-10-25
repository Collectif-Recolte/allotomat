using GraphQL.Language.AST;
using GraphQL.Validation;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Plugins.GraphQL
{
    public class NoIntrospection : IValidationRule
    {
        private static readonly string[] IntrospectionFields = { "__schema", "__type" };

        public Task<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            INodeVisitor visitor = new MatchingNodeVisitor<Field>((field, ctx) =>
            {
                if (!IntrospectionFields.Contains(field.Name)) return;

                context.ReportError(
                    new ValidationError(
                        ctx.Document.OriginalQuery,
                        "no_introspection",
                        "Introspection query is not allowed",
                        field
                    )
                );
            });

            return Task.FromResult(visitor);
        }
    }
}
