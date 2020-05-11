using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Label.Synchronizer.Bot
{
    public static class GitHubWorkerFunc
    {
        [FunctionName("worker")]
        public static async Task RunAsync([QueueTrigger("label-sync-bot-items"), StorageAccount("AzureWebJobsStorage")]CloudQueueMessage message, ILogger log)
        {
            var payload = JObject.Parse(message.AsString)
                                 .GetPayload();
            using var github = GitHubApi.GetGitHubClient();

            // Query Metadata
            await github.InitializeSubscribedPlan(payload, log);
            await github.AuthenticateAsInstallation(payload);

            // Execute event
            switch (payload)
            {
                case LabelCreatedPayload created:
                    await github.HandleLabelCreatedEvent(created, log);
                    return;

                case LabelEditedPayload edited:
                    await github.HandleLabelEditedEvent(edited, log);
                    return;

                case LabelDeletedPayload deleted:
                    await github.HandleLabelDeletedEvent(deleted, log);
                    return;
            };


            log.LogInformation($"C# Queue trigger function processed: {message}");
        }


        #region Created 

        static async Task HandleLabelCreatedEvent(this HttpClient github, LabelCreatedPayload payload, ILogger log)
        {
            var count = 0;
            var owner = payload.OwnerLogin;
            var name = payload.LabelName;
            var color = payload.LabelColor;
            var description = payload.LabelDescription;

            log.LogInformation($"Creating label '{name}' in '{owner}' initiated by '{payload.SenderLogin}'");

            await foreach (var node in github.GetNodes(payload))
            {
                var repo = node[GitHubApi.NAME].Value<string>();
                var content = new StringContent($"{{ \"name\": \"{name}\", \"description\": \"{description}\", \"color\": \"{color}\" }} ");
                var label = node[GitHubApi.LABEL];

                if (null != label && label.HasValues)
                {
                    // Update existing label
                    (await github.PatchAsync($"/repos/{owner}/{repo}/labels/{name}", content))
                                 .EnsureSuccessStatusCode();
                    count += 1;
                }
                else
                {
                    // Create label
                    (await github.PostAsync($"/repos/{owner}/{repo}/labels", content))
                                 .EnsureSuccessStatusCode();
                    count += 1;
                }
            }

            var message = $"Successfully created in {count} repositories";
            log.LogInformation(message);
        }

        #endregion


        #region Edited

        static async Task HandleLabelEditedEvent(this HttpClient github, LabelEditedPayload payload, ILogger log)
        {
            var count = 0;
            var owner = payload.OwnerLogin;
            var name = payload.LabelName;
            var color = payload.LabelColor;
            var changedName = payload.ChangedName;
            var description = payload.LabelDescription;

            log.LogInformation($"Updating label '{name}' in '{owner}' initiated by '{payload.SenderLogin}'");

            // When label is renamed, it requires checks for new and old
            // name so it is split in two possible cases here
            if (null != changedName && changedName.HasValues)
            {
                // Process changes in the name
                await foreach (var node in github.MatchNodes(payload))
                {
                    if (null != node.original)
                    {
                        // This is the case where both, the new label, and the old are present
                        // We need to delete one modify the other
                        var method = $"/repos/{owner}/{node.repo}/labels/{node.original}";
                        (await github.DeleteAsync(method)).EnsureSuccessStatusCode();
                    }

                    if (null != node.label)
                    {
                        // Update existing label
                        var content = new StringContent($"{{ \"new_name\": \"{name}\", \"description\": \"{description}\", \"color\": \"{color}\" }} ");
                        (await github.PatchAsync($"/repos/{owner}/{node.repo}/labels/{node.label}", content))
                                     .EnsureSuccessStatusCode();
                        count += 1;
                    }
                    else
                    {
                        // Create label
                        var content = new StringContent($"{{ \"name\": \"{node.label}\", \"description\": \"{description}\", \"color\": \"{color}\" }} ");
                        (await github.PostAsync($"/repos/{owner}/{node.repo}/labels", content))
                                     .EnsureSuccessStatusCode();
                        count += 1;
                    }
                }
            }
            else
            {
                // Process changes in color or description
                await foreach (var node in github.GetNodes(payload))
                {
                    var repo = node[GitHubApi.NAME].Value<string>();
                    var content = new StringContent($"{{ \"name\": \"{name}\", \"description\": \"{description}\", \"color\": \"{color}\" }} ");
                    var label = node[GitHubApi.LABEL];

                    if (null != label && label.HasValues)
                    {
                        // Update existing label
                        (await github.PatchAsync($"/repos/{owner}/{repo}/labels/{name}", content))
                                     .EnsureSuccessStatusCode();
                        count += 1;
                    }
                    else
                    {
                        // Create label
                        (await github.PostAsync($"/repos/{owner}/{repo}/labels", content))
                                     .EnsureSuccessStatusCode();
                        count += 1;
                    }
                }
            }

            var message = $"Successfully updated in {count} repositories";
            log.LogInformation(message);
        }

        #endregion


        #region Deleted

        static async Task HandleLabelDeletedEvent(this HttpClient github, LabelDeletedPayload payload, ILogger log)
        {
            var count = 0;
            var name = payload.LabelName;
            var owner = payload.OwnerLogin;

            log.LogInformation($"Deleting label '{name}' in '{owner}' initiated by '{payload.SenderLogin}'");

            await foreach (var node in github.GetNodes(payload))
            {
                var label = node[GitHubApi.LABEL];

                if (null == label || !label.HasValues) continue;

                (await github.DeleteAsync($"/repos/{owner}/{node[GitHubApi.NAME].Value<string>()}/labels/{name}"))
                             .EnsureSuccessStatusCode();
                count += 1;
            }


            var message = $"Successfully deleted in {count} repositories";
            log.LogInformation(message);
        }

        #endregion

    }
}
