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
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using SmartThumbnailsBot.Models;

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
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                // Get the saved profile values
                //http://aihelpwebsite.com/Blog/EntryId/8/Introduction-To-FormFlow-With-The-Microsoft-Bot-Framework

                // Get any saved values
                StateClient sc = activity.GetStateClient();
                BotData userData = sc.BotState.GetPrivateConversationData(activity.ChannelId, activity.Conversation.Id, activity.From.Id);

                var boolDataComplete = userData.GetProperty<bool>("DataComplete");

                if (!boolDataComplete)
                {
                    // Call our FormFlow by calling MakeRootDialog
                    await Conversation.SendAsync(activity, MakeRootDialog);
                }
                else {
                    var height = userData.GetProperty<Int64>("Height");
                    var width = userData.GetProperty<Int64>("Width");
                    var smartCrop = userData.GetProperty<bool>("SmartCrop");

                    //var height = 100;
                    //var width = 100;
                    //var smartCrop = true;

                    if (activity.Attachments.Count > 0)
                    {
                        await ResizeImage(connector, activity, height, width, smartCrop);
                    }
                    else
                    {
                        Activity noPictureReply = activity.CreateReply($"Please send me an image.");
                        await connector.Conversations.SendToConversationAsync(noPictureReply);
                    }
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task ResizeImage(ConnectorClient connector, Activity activity, Int64 height, Int64 width, bool smartCrop)
        {
            //get the source image
            var sourceImage = await connector.HttpClient.GetStreamAsync(activity.Attachments.FirstOrDefault().ContentUrl);

            //resize the image using CS
            var resizedImage = await ComputerVisionService.GetImageThumbnail(sourceImage, height, width, smartCrop);

            //upload the image to storage
            var resizedImageBytes = await resizedImage.Content.ReadAsByteArrayAsync();
            var resizedImageFileName = $"{activity.Conversation.Id}-{Guid.NewGuid()}.jpg";
            var storageImageUri = AzureStorageService.Upload(resizedImageBytes, resizedImageFileName);

            //construct reply
            var replyText = (smartCrop == true) ?
                    "I smartly resized an image for you, I'm good like that" :
                    "I resized an image for you, I'm good like that";
            Activity replyToConversation = activity.CreateReply(replyText);
            replyToConversation.Recipient = activity.From;
            replyToConversation.Type = "message";
            replyToConversation.Attachments = new List<Attachment>();

            //add attachment to reply
            var replyFile = new Attachment();
            replyFile.Name = "yournewimage.jpg";
            replyFile.ContentUrl = storageImageUri;
            replyFile.ContentType = "image/jpeg";
            replyToConversation.Attachments.Add(replyFile);

            //send reply
            var reply = await connector.Conversations.SendToConversationAsync(replyToConversation);
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

        public static IForm<ResizeRequest> BuildForm()
        {
            return new FormBuilder<ResizeRequest>()
                    .Message("I just a few details about your new image ...")
                    .OnCompletion(async (context, resizeRequestForm) =>
                    {
                        // Set BotUserData
                        context.PrivateConversationData.SetValue<bool>("DataComplete", true);
                        context.PrivateConversationData.SetValue<Int64>("Height", resizeRequestForm.Height);
                        context.PrivateConversationData.SetValue<Int64>("Width", resizeRequestForm.Width);
                        context.PrivateConversationData.SetValue<bool>("SmartCrop", resizeRequestForm.SmartCrop);

                        // Tell the user that the form is complete
                        await context.PostAsync("OK, thanks, please send me an image now.");

                    })
                    .Build();
        }

        internal static IDialog<ResizeRequest> MakeRootDialog()
        {
            return Chain.From(() => FormDialog.FromForm(BuildForm));
        }
    }




}