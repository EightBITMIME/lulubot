using System;
using System.Collections;
using System.Threading.Tasks;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.ConnectorEx;
using System.Net.Http;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System.Net.Mail;

namespace Microsoft.Bot.Sample.ProactiveBot
{
    [Serializable]
    public class ProactiveDialog : IDialog<object>
    {
        public String incident = "";
        public ArrayList updates = new ArrayList();
        public int updateNum = 0;
        public string[] EmailAddresses = ["lhutchison@textron.com", "jcoles@textron.com", "cschultz@textron.com", "dcyr@textron.com"];
        public String Body = "Lulu Test";
        public string Subject = "Lulu Test";

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            //description of incident
            bool b = message.Text.StartsWith("Incident") || message.Text.StartsWith("incident");
            //update status
            bool c = message.Text.StartsWith("update")  || message.Text.StartsWith("Update");

            bool d = message.Text.Contains("Ok to close") || message.Text.Contains("ok to close");

            bool e = message.Text.StartsWith("Status") || message.Text.StartsWith("status");

            bool email = message.Text.Contains("Send email") || message.Text.Contains("send email");

            // Create a queue Message
            var queueMessage = new Message
                {
                    RelatesTo = context.Activity.ToConversationReference(),
                    Text = message.Text
                };

                // write the queue Message to the queue
                await AddMessageToQueueAsync(JsonConvert.SerializeObject(queueMessage));

            //starting message
            if (message.Text == "I would like to report a security incident")
            {
                await context.PostAsync($"Please describe the security incident starting with the word 'Incident:'");
                context.Wait(MessageReceivedAsync);
            }
            //description of security incident
            else if(b){
                incident = message.Text;
                await context.PostAsync($"Incident has been recorded. In order to add updates please type 'update' followed by the update.");
                await context.PostAsync($"In order to get the status of the incident, please type 'Status'. In order to close the incident please type 'Ok to close'");
                context.Wait(MessageReceivedAsync);
            }
            //update status
            else if (c)
            {
                updates.Add(updateNum + " : " + message.Text);
                updateNum++;
                await context.PostAsync($"Status updated");
                context.Wait(MessageReceivedAsync);
            }
            //closing incident
            else if (d)
            {
                await context.PostAsync($"The ticket is now closed.");
                incident = "";
                updates.Clear();
                context.Wait(MessageReceivedAsync);
            }
            else if (e)
            {
                await context.PostAsync($"These are the reported updates:");
                for (int i = 0; i < updates.Count; i++)
                {
                    await context.PostAsync(updates[i].ToString());
                }
            }
            else if (email)
            {
                await context.PostAsync($"Sending emails to all participants");
                for (int j = 0; j < EmailAddresses.Length; j++)
                {
                    SendEmail(EmailAddresses[j], " ", "svc_hq_o365_test_bo@txt.textron.com", "Lulu", Subject, Body, false);
                }
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

        public static bool SendEmail(string To, string ToName, string From, string FromName, string Subject, string Body, bool IsBodyHTML)
        {
            try
            {
                MailAddress FromAddr = new MailAddress(From, FromName, System.Text.Encoding.UTF8);
                MailAddress ToAddr = new MailAddress(To, ToName, System.Text.Encoding.UTF8);
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 25,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential("your email address", "your password")
                };

                using (MailMessage message = new MailMessage(FromAddr, ToAddr)
                {
                    Subject = Subject,
                    Body = Body,
                    IsBodyHtml = IsBodyHTML,
                    BodyEncoding = System.Text.Encoding.UTF8,

                })
                {
                    smtp.Send(message);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}