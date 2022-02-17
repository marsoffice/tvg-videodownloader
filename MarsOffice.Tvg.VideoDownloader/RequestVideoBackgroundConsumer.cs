using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarsOffice.Tvg.VideoDownloader.Abstractions;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Queue.Protocol;
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
            [QueueTrigger("request-videobackground", Connection = "localsaconnectionstring")] QueueMessage message,
            [Queue("videobackground-result", Connection = "localsaconnectionstring")] IAsyncCollector<VideoBackgroundResult> videoBackgroundResultQueue,
            ILogger log)
        {
            var request = Newtonsoft.Json.JsonConvert.DeserializeObject<RequestVideoBackground>(message.Text,
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                    });
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

                var fileNameSplit = selectedBlob.StorageUri.PrimaryUri.LocalPath.ToString().Split("/");
                var fileName = fileNameSplit.Last();
                var filePath = "video/" + fileName;

                await videoBackgroundResultQueue.AddAsync(new VideoBackgroundResult
                {
                    VideoId = request.VideoId,
                    Success = true,
                    JobId = request.JobId,
                    UserEmail = request.UserEmail,
                    UserId = request.UserId,
                    FileLink = filePath,
                    Category = request.Category,
                    LanguageCode = request.LanguageCode,
                    FileName = fileName
                });
                await videoBackgroundResultQueue.FlushAsync();
            }
            catch (Exception e)
            {
                log.LogError(e, "Function threw an error");
                if (message.DequeueCount >= 5)
                {
                    await videoBackgroundResultQueue.AddAsync(new VideoBackgroundResult
                    {
                        VideoId = request.VideoId,
                        Success = false,
                        Error = "VideoDownloaderService: " + e.Message,
                        JobId = request.JobId,
                        UserEmail = request.UserEmail,
                        UserId = request.UserId,
                        Category = request.Category,
                        LanguageCode = request.LanguageCode
                    });
                    await videoBackgroundResultQueue.FlushAsync();
                }
                throw;
            }
        }
    }
}
