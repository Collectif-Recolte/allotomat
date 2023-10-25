using GraphQL.Conventions;

namespace Sig.App.Backend.Utilities.Sorting
{
    [InputType]
    public class Sort<TSortField> where TSortField : struct
    {
        public TSortField Field { get; set; }
        public SortOrder Order { get; set; }
    }
}