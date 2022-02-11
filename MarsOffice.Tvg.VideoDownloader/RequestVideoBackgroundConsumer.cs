using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarsOffice.Tvg.VideoDownloader.Abstractions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarsOffice.Tvg.VideoDownloader
{
    public class RequestVideoBackgroundConsumer
    {
        private readonly IConfiguration _config;

        public RequestVideoBackgroundConsumer(IConfiguration config)
        {
            _config = config;
        }

        [FunctionName("RequestVideoBackgroundConsumer")]
        public async Task Run(
            [QueueTrigger("request-videobackground", Connection = "localsaconnectionstring")]RequestVideoBackground request, 
            [Queue("videobackground-result", Connection = "localsaconnectionstring")] IAsyncCollector<VideoBackgroundResult> videoBackgroundResultQueue,
            ILogger log)
        {
            try
            {
                var cloudStorageAccount = CloudStorageAccount.Parse(_config["localsaconnectionstring"]);
                var blobClient = cloudStorageAccount.CreateCloudBlobClient();
                var containerReference = blobClient.GetContainerReference("video");
#if DEBUG
                await containerReference.CreateIfNotExistsAsync();
#endif
                BlobContinuationToken bct = null;
                var hasData = true;
                var blobs = new List<IListBlobItem>();

                while (hasData)
                {
                    var allFilesInContainer = await containerReference.ListBlobsSegmentedAsync(bct);
                    blobs.AddRange(allFilesInContainer.Results);
                    bct = allFilesInContainer.ContinuationToken;
                    if (bct == null)
                    {
                        hasData = false;
                    }
                }

                if (blobs.Count == 0)
                {
                    throw new Exception("No audio files present on server");
                }

                var rand = new Random();
                int randomIndex = rand.Next(0, blobs.Count);

                var selectedBlob = blobs[randomIndex];

                await videoBackgroundResultQueue.AddAsync(new VideoBackgroundResult
                {
                    VideoId = request.VideoId,
                    Success = true,
                    JobId = request.JobId,
                    UserEmail = request.UserEmail,
                    UserId = request.UserId,
                    FileLink = selectedBlob.StorageUri.PrimaryUri.ToString(),
                    Category = request.Category,
                    LanguageCode = request.LanguageCode,
                    FileName = selectedBlob.StorageUri.PrimaryUri.LocalPath
                });
                await videoBackgroundResultQueue.FlushAsync();
            }
            catch (Exception e)
            {
                await videoBackgroundResultQueue.AddAsync(new VideoBackgroundResult
                {
                    VideoId = request.VideoId,
                    Success = false,
                    Error = e.Message,
                    JobId = request.JobId,
                    UserEmail = request.UserEmail,
                    UserId = request.UserId,
                    Category = request.Category,
                    LanguageCode = request.LanguageCode
                });
                await videoBackgroundResultQueue.FlushAsync();
            }
        }
    }
}
