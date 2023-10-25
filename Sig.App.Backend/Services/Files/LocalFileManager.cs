using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace Sig.App.Backend.Services.Files
{
    public class LocalFileManager : IFileManager
    {
        private const string ContentTypeExtension = ".contenttype";
        private const string MetadataExtension = ".metadata";
        private const string BaseUploadFolder = "_uploaded-files";
        
        private readonly string contentRoot;

        public LocalFileManager(IWebHostEnvironment hosting)
        {
            contentRoot = hosting.ContentRootPath;
        }

        public async Task<string> UploadFile(string container, FileInfos file, bool useFileNameAsFileId = false)
        {
            var fileId = useFileNameAsFileId 
                ? file.FileName 
                : $"{Guid.NewGuid()}/{file.FileName}";
            
            var targetFilePath = Path.Combine(GetUploadFolder(container), fileId);

            var info = new FileInfo(targetFilePath);
            if (!Directory.Exists(info.DirectoryName)) Directory.CreateDirectory(info.DirectoryName);

            using (var targetFile = info.OpenWrite())
            {
                await file.Content.CopyToAsync(targetFile);
            }

            await WriteContentType(file, targetFilePath);
            await WriteMetadata(file, targetFilePath);

            return fileId;
        }

        public async Task<FileInfos> DownloadFile(string container, string fileId)
        {
            var targetFilePath = Path.Combine(GetUploadFolder(container), fileId);
            var info = new FileInfo(targetFilePath);
            if (!info.Exists) return null;

            var stream = new MemoryStream();

            using (var fileStream = info.OpenRead())
            {
                await fileStream.CopyToAsync(stream);
            }

            stream.Position = 0;

            var contentType = await ReadContentType(targetFilePath);
            var metadata = await ReadMetadata(targetFilePath);

            return new FileInfos
            {
                FileName = info.Name,
                ContentType = contentType,
                Content = stream,
                Metadata = metadata
            };
        }

        public Task DeleteFile(string container, string fileId)
        {
            var targetFilePath = Path.Combine(GetUploadFolder(container), fileId);
            
            DeleteIfExists(targetFilePath);
            DeleteIfExists(targetFilePath + ContentTypeExtension);
            DeleteIfExists(targetFilePath + MetadataExtension);

            return Task.CompletedTask;

            void DeleteIfExists(string path)
            {
                var info = new FileInfo(path);
                if (info.Exists)
                    info.Delete();
            }
        }

        public async Task<FileAttributes> GetFileAttributes(string container, string fileId)
        {
            var targetFilePath = Path.Combine(GetUploadFolder(container), fileId);
            var info = new FileInfo(targetFilePath);
            if (!info.Exists) return null;

            return new FileAttributes
            {
                LastWriteTime = info.LastWriteTimeUtc,
                Length = info.Length,
                Metadata = await ReadMetadata(targetFilePath)
            };
        }

        public Task<Stream> GetFileStream(string container, string fileId)
        {
            var targetFilePath = Path.Combine(GetUploadFolder(container), fileId);
            var info = new FileInfo(targetFilePath);
            if (!info.Exists) return Task.FromResult<Stream>(null);

            return Task.FromResult<Stream>(info.OpenRead());
        }

        public Task<bool> Exists(string container, string fileId)
        {
            var targetFilePath = Path.Combine(GetUploadFolder(container), fileId);
            var info = new FileInfo(targetFilePath);
            return Task.FromResult(info.Exists);
        }
        
        public Task<string> GetFileUrl(string container, string fileId)
        {
            return Task.FromResult(Path.Combine(GetUploadFolder(container), fileId));
        }

        private string GetUploadFolder(string subFolder)
        {
            return Path.Combine(contentRoot, BaseUploadFolder, subFolder);
        }

        private static async Task<string> ReadContentType(string targetFilePath)
        {
            string contentType = null;
            if (File.Exists(targetFilePath + ContentTypeExtension))
            {
                contentType = await File.ReadAllTextAsync(targetFilePath + ContentTypeExtension);
            }

            return contentType;
        }

        private static async Task<IDictionary<string, string>> ReadMetadata(string targetFilePath)
        {
            IDictionary<string, string> metadata = null;
            if (File.Exists(targetFilePath + MetadataExtension))
            {
                metadata = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    await File.ReadAllTextAsync(targetFilePath + MetadataExtension));
            }

            return metadata;
        }

        private static async Task WriteContentType(FileInfos file, string targetFilePath)
        {
            await File.WriteAllTextAsync(targetFilePath + ContentTypeExtension, file.ContentType);
        }

        private static async Task WriteMetadata(FileInfos file, string targetFilePath)
        {
            if (file.Metadata != null)
            {
                await File.WriteAllTextAsync(
                    targetFilePath + MetadataExtension,
                    JsonConvert.SerializeObject(file.Metadata));
            }
        }
    }
}