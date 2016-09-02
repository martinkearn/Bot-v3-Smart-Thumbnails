using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SmartThumbnailsBot.Models
{
    [Serializable]
    public class ResizeRequest
    {
        [Prompt("How high?")]
        public int Height { get; set; }

        [Prompt("How wide?")]
        public int Width { get; set; }

        [Prompt("Do you want it cropped on the region of interest?")]
        public bool SmartCrop { get; set; }
    }
}