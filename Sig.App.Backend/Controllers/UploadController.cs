using System;
using System.IO;
using System.Threading.Tasks;
using MediatR;
using Sig.App.Backend.Constants;
using Sig.App.Backend.Services.Files;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Sig.App.Backend.Requests.Commands;

namespace Sig.App.Backend.Controllers
{
    [Route("upload")]
    [Authorize] // Seulement les utilisateurs identifiés peuvent uploader.
    public class UploadController : Controller
    {
        // Taille maximale utilisée pour écran rétina
        private const int MaxImageWidth = (int)(1920 * 1.5);
        private const int MaxImageHeight = (int)(1080 * 1.5);

        private readonly IFileManager fileManager;
        private readonly IMediator mediator;
        private readonly ILogger<UploadController> logger;

        public UploadController(IFileManager fileManager, IMediator mediator, ILogger<UploadController> logger)
        {
            this.fileManager = fileManager;
            this.mediator = mediator;
            this.logger = logger;
        }

        [HttpPost("tempFile")]
        public async Task<IActionResult> UploadTempFile(IFormFile file)
        {
            if (file == null) return BadRequest();

            var result = await mediator.Send(new SaveTemporaryFile.Command
            {
                File = new FileInfos
                {
                    Content = file.OpenReadStream(),
                    ContentType = file.ContentType,
                    FileName = file.FileName
                }
            });

            return Ok(new
            {
                fileId = result.FileId
            });
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null) return BadRequest();

            FileInfos fileInfos;

            try
            {
                fileInfos = await GetImageFileInfos(file);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error pre-processing uploaded image");
                return BadRequest();
            }

            var fileId = await fileManager.UploadFile(FileContainers.Images, fileInfos);
            logger.LogInformation($"Image uploaded ({fileId})");

            return Ok(new
            {
                fileId
            });
        }

        private static async Task<FileInfos> GetImageFileInfos(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            using (var image = await Image.LoadAsync(stream))
            {
                if (image.Width > MaxImageWidth || image.Height > MaxImageHeight)
                {
                    image.Mutate(x =>
                        x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(MaxImageWidth, MaxImageHeight)
                        }));
                }

                var fileInfos = new FileInfos { Content = new MemoryStream() };

                if (file.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    await image.SaveAsPngAsync(fileInfos.Content);
                    fileInfos.ContentType = "image/png";
                    fileInfos.FileName = file.FileName;
                }
                else
                {
                    await image.SaveAsJpegAsync(fileInfos.Content);
                    fileInfos.ContentType = "image/jpeg";
                    fileInfos.FileName = file.FileName;
                }

                fileInfos.Content.Position = 0;

                return fileInfos;
            }

        }
    }
}