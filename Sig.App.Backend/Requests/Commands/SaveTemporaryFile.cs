using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using MediatR;
using Sig.App.Backend.Constants;
using Sig.App.Backend.Services.Files;

namespace Sig.App.Backend.Requests.Commands;

public class SaveTemporaryFile : IRequestHandler<SaveTemporaryFile.Command, SaveTemporaryFile.Payload>
{
    private const int FileLifetimeInMinutes = 60;

    private readonly IFileManager fileManager;
    private readonly IBackgroundJobClient backgroundJobClient;
    private readonly FileUrlProvider fileUrlProvider;

    public SaveTemporaryFile(IFileManager fileManager, IBackgroundJobClient backgroundJobClient, FileUrlProvider fileUrlProvider)
    {
        this.fileManager = fileManager;
        this.backgroundJobClient = backgroundJobClient;
        this.fileUrlProvider = fileUrlProvider;
    }

    public async Task<Payload> Handle(Command request, CancellationToken cancellationToken)
    {
        var fileId = await fileManager.UploadFile(FileContainers.TemporaryFiles, request.File);
        var fileLifetime = TimeSpan.FromMinutes(FileLifetimeInMinutes);

        backgroundJobClient.Schedule(() => fileManager.DeleteFile(FileContainers.TemporaryFiles, fileId), fileLifetime);

        return new Payload
        {
            FileId = fileId,
            FileUrl = fileUrlProvider.GetFileUrl(FileContainers.TemporaryFiles, fileId, fileLifetime)
        };
    }

    public class Command : IRequest<Payload>
    {
        public FileInfos File { get; set; }
    }

    public class Payload
    {
        public string FileId { get; set; }
        public string FileUrl { get; set; }
    }
}