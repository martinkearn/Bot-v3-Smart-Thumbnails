using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace SmartThumbnailsBot.Services
{
    public static class ComputerVisionService
    {
        //_apiUrl: The base URL for the API. Find out what this is for other APIs via the API documentation
        public const string _apiUrlBase = "https://api.projectoxford.ai/vision/v1.0/generateThumbnail";

        public async static Task<HttpResponseMessage> GetImageThumbnail(Stream sourceImage, Int64 height, Int64 width, bool smartCropping)
        {
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient
                httpClient.BaseAddress = new Uri(_apiUrlBase);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ConfigurationManager.AppSettings["ComputerVisionAPIKey"]);

                //setup data object
                HttpContent content = new StreamContent(sourceImage);
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                // Request parameters
                var uri = $"{_apiUrlBase}?width={width}&height={height}&smartCropping={smartCropping}";

                //make request
                return await httpClient.PostAsync(uri, content);
            }
        }

    }
}