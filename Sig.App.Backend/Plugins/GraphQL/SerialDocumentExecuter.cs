using GraphQL;
using GraphQL.Execution;
using GraphQL.Language.AST;


namespace Sig.App.Backend.Plugins.GraphQL
{
    public class SerialDocumentExecuter : DocumentExecuter
    {
        protected override IExecutionStrategy SelectExecutionStrategy(ExecutionContext context)
        {
            return context.Operation.OperationType == OperationType.Query
                ? new SerialExecutionStrategy()
                : base.SelectExecutionStrategy(context);
        }
    }
}