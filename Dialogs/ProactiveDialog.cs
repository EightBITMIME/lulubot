using System;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.ConnectorEx;
using System.Net.Http;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Microsoft.Bot.Sample.ProactiveBot
{
    [Serializable]
    public class ProactiveDialog : IDialog<object>
    {
        public String incident = "";

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            
                // Create a queue Message
                //test comment
                var queueMessage = new Message
                {
                    RelatesTo = context.Activity.ToConversationReference(),
                    Text = message.Text
                };

                // write the queue Message to the queue
                await AddMessageToQueueAsync(JsonConvert.SerializeObject(queueMessage));

            if (message.Text == "I would like to report a security incident")
            {
                await context.PostAsync($"Please describe the security incident");
                context.Wait(MessageReceivedAsync);

                incident = message.Text;
            }
            else
            {
                await context.PostAsync($"<sarcastic mocking> {queueMessage.Text}.");
                context.Wait(MessageReceivedAsync);
            }
        }

        public static async Task AddMessageToQueueAsync(string message)
        {
            // Retrieve storage account from connection string.
            var storageAccount = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["AzureWebJobsStorage"]); // If you're running this bot locally, make sure you have this appSetting in your web.config

            // Create the queue client.
            var queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue.
            var queue = queueClient.GetQueueReference("bot-queue");

            // Create the queue if it doesn't already exist.
            await queue.CreateIfNotExistsAsync();

            // Create a message and add it to the queue.
            var queuemessage = new CloudQueueMessage(message);
            await queue.AddMessageAsync(queuemessage);
        }

    }
}