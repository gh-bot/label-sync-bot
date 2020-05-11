using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Label.Synchronizer.Bot
{
    public static class GitHubWebhookFunc
    {
        [FunctionName("webhook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger("post", Route = null)] HttpRequest request, 
            //[Queue("label-sync-bot-items", Connection = "StorageConnectionString")]CloudQueue outputQueue,
            [Queue("label-sync-bot-items"), StorageAccount("AzureWebJobsStorage")] CloudQueue outputQueue,
            ILogger log)
        {
            var debugging = log.IsEnabled(LogLevel.Debug);

            // Ignore unhandled requests
            var ghEvent = request.Headers.ValueOrDefault("X-GitHub-Event");
            if (!GitHubApi.LABEL.Equals(ghEvent))
            {
                if (debugging) log.LogDebug($"IGNORED: X-GitHub-Event: '{ghEvent}'");
                return new OkResult();
            }

            // Ignore events from bots
            var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            if (Regex.IsMatch(requestBody, "(\"type\")\\s*:\\s*(\"Bot\")"))
            {
                if (debugging) log.LogDebug($"IGNORED: X-GitHub-Event: '{ghEvent}' from bot");
                return new OkResult();
            }

            // Add message to queue
            var delivery = request.Headers.ValueOrDefault("X-GitHub-Delivery");
            var message  = new CloudQueueMessage(delivery, null);

            message.SetMessageContent(requestBody);
            await outputQueue.AddMessageAsync(message);
            log.LogInformation($"Queued '{delivery}' event for processing");

            // Report success
            return new OkObjectResult("ACCEPTED");
        }
    }
}
