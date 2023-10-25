using System.Collections.Generic;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public interface IIdListQuery<TId>
    {
        IEnumerable<TId> Ids { get; set; }
    }

    public interface IHaveGroup<TGroup>
    {
        TGroup Group { get; set; }
    }
}