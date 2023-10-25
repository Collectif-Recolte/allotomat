using Sig.App.Backend.Gql.Schema.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Gql.Schema.Types
{
    public class VerifyTokenPayload
    {
        public TokenStatus Status { get; set; }
        public UserGraphType User { get; set; }
    }
}
