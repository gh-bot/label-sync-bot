using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
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
            [Queue("label-sync-bot-items"), StorageAccount("AzureWebJobsStorage")] ICollector<string> queue,
            ILogger log)
        {
            var debugging = log.IsEnabled(LogLevel.Debug);

            // Ignore unhandled requests
            var ghEvent = request.Headers.ValueOrDefault("X-GitHub-Event");
            if (!GitHubApi.LABEL.Equals(ghEvent))
            {
                if (debugging) log.LogDebug($"IGNORED: X-GitHub-Event: '{ghEvent}'. X-GitHub-Delivery: {request.Headers.ValueOrDefault("X-GitHub-Delivery")} ");
                return new OkResult();
            }

            var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            if (log.IsEnabled(LogLevel.Trace))
            {
                if (log.IsEnabled(LogLevel.Trace)) log.LogTrace($"Request:\n {requestBody}");
            }

            // Ignore events from bots
            if (Regex.IsMatch(requestBody, "(\"type\")\\s*:\\s*(\"Bot\")"))
            {
                if (debugging) log.LogDebug($"IGNORED X-GitHub-Delivery: {request.Headers.ValueOrDefault("X-GitHub-Delivery")} from BOT");
                return new OkResult();
            }

            // Add message to queue
            queue.Add(requestBody);

            var delivery = request.Headers.ValueOrDefault("X-GitHub-Delivery");
            log.LogInformation($"Queued '{delivery}' event for processing");

            // Report success
            return new OkObjectResult("ACCEPTED");
        }
    }
}
