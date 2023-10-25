using GraphQL.Conventions;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Services.Files;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class ImageGraphType
    {
        private readonly string fileId;

        public ImageGraphType(string fileId)
        {
            this.fileId = fileId;
        }

        public Id Id => Id.New<ImageGraphType>(fileId);
        public string Url([Inject] ImageUrlProvider urlProvider, ImageFormat format)
        {
            return urlProvider.GetImageUrl(fileId, format);
        }
    }
}