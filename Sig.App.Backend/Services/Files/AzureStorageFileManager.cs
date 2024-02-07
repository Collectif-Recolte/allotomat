using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sig.App.Backend.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using SixLabors.ImageSharp.Web.Commands;

namespace Sig.App.Backend.Services.Files
{
    public class AzureStorageFileManager : IFileManager
    {
        private readonly ILogger<AzureStorageFileManager> logger;
        private readonly CloudStorageAccount account;

        public AzureStorageFileManager(IOptions<AzureStorageFileManagerOptions> options, ILogger<AzureStorageFileManager> logger)
        {
            this.logger = logger;
            
            account = CloudStorageAccount.Parse(options.Value.ConnectionString);
        }

        public async Task<string> UploadFile(string container, FileInfos file, bool useFileNameAsFileId = false)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            try
            {
                var blobName = useFileNameAsFileId
                    ? file.FileName
                    : $"{Guid.NewGuid()}/{Regex.Replace(file.FileName, "[^a-z0-1.]", "", RegexOptions.IgnoreCase)}";
                
                var blob = await GetBlob(container, blobName);

                await blob.UploadFromStreamAsync(file.Content);
                
                blob.Metadata.Add("ContentType", file.ContentType);
                blob.Metadata.Add("OriginalFileNameB64", file.FileName.ToBase64());
                
                if (file.Metadata != null)
                {
                    foreach (var (key, value) in file.Metadata)
                    {
                        if (blob.Metadata.ContainsKey(key)) continue;
                        blob.Metadata.Add(key, value);
                    }
                }
                
                await blob.SetMetadataAsync();

                return blobName;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error uploading file to Azure Storage (filename: {file.FileName})");
                throw;
            }
        }

        public async Task<FileInfos> DownloadFile(string container, string fileId)
        {
            var blob = await GetBlob(container, fileId);
            if (!await blob.ExistsAsync()) return null;

            await blob.FetchAttributesAsync();

            var stream = new MemoryStream();
            await blob.DownloadToStreamAsync(stream);
            stream.Position = 0;

            string originalFileName;
            blob.Metadata.TryGetValue("OriginalFileNameB64", out originalFileName);
            originalFileName = originalFileName?.Base64Decode();

            string contentType;
            blob.Metadata.TryGetValue("ContentType", out contentType);

            return new FileInfos {
                ContentType = contentType,
                FileName = originalFileName,
                Content = stream,
                Metadata = blob.Metadata
            };
        }

        public async Task DeleteFile(string container, string fileId)
        {
            var blob = await GetBlob(container, fileId);
            await blob.DeleteIfExistsAsync();
        }

        public async Task<FileAttributes> GetFileAttributes(string container, string fileId)
        {
            var blob = await GetBlob(container, fileId);
            if (!await blob.ExistsAsync()) return null;
            
            await blob.FetchAttributesAsync();
            return new FileAttributes
            {
                LastWriteTime = blob.Properties.LastModified?.UtcDateTime,
                Length = blob.Properties.Length,
                Metadata = blob.Metadata
            };
        }

        public async Task<Stream> GetFileStream(string container, string fileId)
        {
            var blob = await GetBlob(container, fileId);
            return await blob.OpenReadAsync();
        }

        public async Task<bool> Exists(string container, string fileId)
        {
            var blob = await GetBlob(container, fileId);
            return await blob.ExistsAsync();
        }

        public async Task<string> GetFileUrl(string container, string fileId)
        {
            var blob = await GetBlob(container, fileId);
            return blob.Uri.AbsoluteUri;
        }

        private CloudBlobClient GetClient() => new CloudBlobClient(account.BlobStorageUri, account.Credentials);

        private async Task<CloudBlobContainer> GetContainer(string containerName)
        {
            var client = GetClient();
            var container = client.GetContainerReference(containerName);

            await container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null);
            
            return container;
        }
        
        private async Task<CloudBlockBlob> GetBlob(string containerName, string blobName)
        {
            var container = await GetContainer(containerName);
            return container.GetBlockBlobReference(blobName);
        }
    }
}