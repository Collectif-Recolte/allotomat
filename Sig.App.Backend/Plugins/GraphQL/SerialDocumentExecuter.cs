using GraphQL;
using GraphQL.Execution;
using GraphQLParser.AST;


namespace Sig.App.Backend.Plugins.GraphQL
{
    public class SerialDocumentExecuter : DocumentExecuter
    {
        protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            return context.Operation.Operation == OperationType.Query
                ? new SerialExecutionStrategy()
                : base.SelectExecutionStrategy(context);
        }
    }
}