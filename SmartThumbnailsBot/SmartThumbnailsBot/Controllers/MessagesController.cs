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
                    var smartCropping = userData.GetProperty<bool>("SmartCrop");

                    if (activity.Attachments.Count > 0)
                    {
                        //get the source image
                        var sourceImage = await connector.HttpClient.GetStreamAsync(activity.Attachments.FirstOrDefault().ContentUrl);

                        //resize the image using the cognitive services computer vision api
                        var resizedImage = await ComputerVisionService.GetImageThumbnail(sourceImage, height, width, smartCropping);

                        //construct reply
                        var replyText = (smartCropping == true) ?
                                "I smartly resized an image for you, I'm good like that" :
                                "I resized an image for you, I'm good like that";
                        Activity replyToConversation = activity.CreateReply(replyText);
                        replyToConversation.Recipient = activity.From;
                        replyToConversation.Type = "message";
                        replyToConversation.Attachments = new List<Attachment>();

                        //add attachment to reply
                        var replyFile = new Attachment();
                        var image = "data:image/png;base64," + Convert.ToBase64String(resizedImage);
                        replyToConversation.Attachments.Add(new Attachment { ContentUrl = image, ContentType = "image/png" });

                        //send reply
                        var reply = await connector.Conversations.SendToConversationAsync(replyToConversation);
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
                    .Message("I just need a few details about your new image ...")
                    .OnCompletion(async (context, resizeRequestForm) =>
                    {
                        // Set BotUserData
                        context.PrivateConversationData.SetValue<bool>("DataComplete", true);
                        context.PrivateConversationData.SetValue<Int64>("Height", resizeRequestForm.Height);
                        context.PrivateConversationData.SetValue<Int64>("Width", resizeRequestForm.Width);
                        context.PrivateConversationData.SetValue<bool>("SmartCrop", resizeRequestForm.SmartCrop);

                        // Tell the user that the form is complete
                        await context.PostAsync("All set, please send me an image now.");

                    })
                    .Build();
        }

        internal static IDialog<ResizeRequest> MakeRootDialog()
        {
            return Chain.From(() => FormDialog.FromForm(BuildForm));
        }
    }




}