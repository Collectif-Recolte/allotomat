using GraphQL.Validation;
using GraphQLParser.AST;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Plugins.GraphQL
{
    public class NoIntrospection : IValidationRule
    {
        private static readonly string[] IntrospectionFields = { "__schema", "__type" };

        public ValueTask<INodeVisitor> ValidateAsync(ValidationContext context)
        {
            INodeVisitor visitor = new MatchingNodeVisitor<GraphQLField>((field, ctx) =>
            {
                if (!IntrospectionFields.Contains(field.Name.StringValue)) return;

                context.ReportError(
                    new ValidationError(
                        ctx.Document.Source,
                        "no_introspection",
                        "Introspection query is not allowed",
                        field
                    )
                );
            });
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            return ValueTask.FromResult<INodeVisitor?>(visitor);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        }
    }
}
