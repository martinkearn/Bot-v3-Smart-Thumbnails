using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http.Headers;
using System.Collections.Generic;
using SmartThumbnailsBot.Services;

namespace SmartThumbnailsBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {


        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                if (activity.Attachments.Count > 0)
                {
                    // TO DO look at form flow to get height and width: https://docs.botframework.com/en-us/csharp/builder/sdkreference/forms.html
                    var requestedHeight = 100;
                    var requestedWidth = 100;
                    var smartCropping = true;

                    foreach (var attachment in activity.Attachments)
                    {
                        //get the source image
                        var sourceImage = await connector.HttpClient.GetStreamAsync(attachment.ContentUrl);

                        //resize the image using CS
                        var resizedImage = await ComputerVisionService.GetImageThumbnail(sourceImage, requestedHeight, requestedWidth, smartCropping);

                        //upload the image to storage
                        var resizedImageBytes = await resizedImage.Content.ReadAsByteArrayAsync();
                        var resizedImageFileName = activity.Conversation.Id + Guid.NewGuid() + ".jpg";
                        var storageImageUri = AzureStorageService.Upload(resizedImageBytes, resizedImageFileName);

                        //construct reply
                        Activity replyToConversation = activity.CreateReply("I smartly resized an image for you, I'm good like that");
                        replyToConversation.Recipient = activity.From;
                        replyToConversation.Type = "message";
                        replyToConversation.Attachments = new List<Attachment>();

                        //add attachment to reply
                        var replyFile = new Attachment();
                        replyFile.Name = "YourNewImage.jpg";
                        replyFile.ContentUrl = storageImageUri;
                        replyFile.ContentType = "image/jpeg";
                        replyToConversation.Attachments.Add(replyFile);
                       
                        //send reply
                        var reply = await connector.Conversations.SendToConversationAsync(replyToConversation);
                    }
                }
                else
                {
                    // TO DO reply asking user for an image
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}