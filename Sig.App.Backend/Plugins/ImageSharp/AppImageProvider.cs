using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Sig.App.Backend.Constants;
using Sig.App.Backend.Services.Files;
using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;

namespace Sig.App.Backend.Plugins.ImageSharp
{
    public class AppImageProvider : IImageProvider
    {
        private readonly IOptions<AppImageProviderOptions> options;
        private readonly IFileManager fileManager;

        public AppImageProvider(IOptions<AppImageProviderOptions> options, IFileManager fileManager)
        {
            this.options = options;
            this.fileManager = fileManager;
        }

        public bool IsValidRequest(HttpContext context)
        {
            return context.Request.Path.ToString().StartsWith(options.Value.RequestPrefix);
        }

        public async Task<IImageResolver> GetAsync(HttpContext context)
        {
            var fileId = context.Request.Path.ToString().Substring(options.Value.RequestPrefix.Length);
            // Supports images which name contains spaces
            var decodedFileId = WebUtility.UrlDecode(fileId);

            if (await fileManager.GetFileAttributes(FileContainers.Images, decodedFileId) == null) return null;

            return new AppImageResolver(fileManager, FileContainers.Images, decodedFileId);
        }

        public Func<HttpContext, bool> Match { get; set; } = _ => true;
        public ProcessingBehavior ProcessingBehavior => ProcessingBehavior.All;
    }
}