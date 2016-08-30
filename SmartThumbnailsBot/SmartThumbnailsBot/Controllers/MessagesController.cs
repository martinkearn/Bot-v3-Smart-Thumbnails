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
                    foreach (var attachment in activity.Attachments)
                    {
                        var sourceImage = await connector.HttpClient.GetStreamAsync(attachment.ContentUrl);

                        var resizedImageBytes = await ComputerVisionService.GetImageThumbnailBytes(sourceImage, 100, 100);

                        Activity replyToConversation = activity.CreateReply("I smartly resized an image for you, I'm good like that");
                        replyToConversation.Recipient = activity.From;
                        replyToConversation.Type = "message";
                        replyToConversation.Attachments = new List<Attachment>();

                        List<CardImage> cardImages = new List<CardImage>();
                        cardImages.Add(new CardImage(url: "https://upload.wikimedia.org/wikipedia/en/a/a6/Bender_Rodriguez.png"));

                        List<CardAction> cardButtons = new List<CardAction>();

                        CardAction plButton = new CardAction()
                        {
                            Value = "https://en.wikipedia.org/wiki/Pig_Latin",
                            Type = "openUrl",
                            Title = "WikiPedia Page"
                        };
                        cardButtons.Add(plButton);

                        ThumbnailCard plCard = new ThumbnailCard()
                        {
                            Title = "I'm a thumbnail card",
                            Subtitle = "Pig Latin Wikipedia Page",
                            Images = cardImages,
                            Buttons = cardButtons
                        };

                        Attachment plAttachment = plCard.ToAttachment();
                        replyToConversation.Attachments.Add(plAttachment);

                        var reply = await connector.Conversations.SendToConversationAsync(replyToConversation);

                    }
                }
                else
                {
                    //reply asking user for an image
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private string BytesToSrcString(byte[] bytes) => "data:image/jpg;base64," + Convert.ToBase64String(bytes);

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