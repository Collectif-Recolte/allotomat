using System;
using System.IO;
using System.Threading.Tasks;
using Sig.App.Backend.Services.Files;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Resolvers;

namespace Sig.App.Backend.Plugins.ImageSharp
{
    public class AppImageResolver : IImageResolver, IImageCacheResolver
    {
        private readonly IFileManager fileManager;
        private readonly string container;
        private readonly string fileId;

        public AppImageResolver(IFileManager fileManager, string container, string fileId)
        {
            this.fileManager = fileManager;
            this.container = container;
            this.fileId = fileId;
        }

        async Task<ImageMetadata> IImageResolver.GetMetaDataAsync()
        {
            var attrs = await fileManager.GetFileAttributes(container, fileId);
            return new ImageMetadata(attrs?.LastWriteTime ?? DateTime.MinValue, attrs?.Length ?? 0);
        }

        async Task<ImageCacheMetadata> IImageCacheResolver.GetMetaDataAsync()
        {
            var attrs = await fileManager.GetFileAttributes(container, fileId);
            return ImageCacheMetadata.FromDictionary(attrs.Metadata);
        }

        public Task<Stream> OpenReadAsync()
        {
            return fileManager.GetFileStream(container, fileId);
        }
    }
}