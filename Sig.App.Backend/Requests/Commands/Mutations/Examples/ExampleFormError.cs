using MediatR;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Examples
{
    public class ExampleFormError : IRequestHandler<ExampleFormError.Input, ExampleFormError.Payload>
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            // Return an error when the country do not exist
            if (request.Country == "impossible-country")
            {
                throw new CountryNotAcceptedException();
            }

            return new Payload
            {
                Success = true
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public string Username { get; set; }
            public string About { get; set; }
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Password { get; set; }
            public string Country { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string Province { get; set; }
            public string PostalCode { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public bool Success { get; set; }
        }

        public class CountryNotAcceptedException : RequestValidationException { }
    }
}
