using System;
using System.Collections.Generic;
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
        //_apiKey: Replace this with your own Cognitive Services Computer Vision API key, please do not use my key. I include it here so you can get up and running quickly but you can get your own key for free at https://www.microsoft.com/cognitive-services/en-us/computer-vision-api
        public const string _apiKey = "382f5abd65f74494935027f65a41a4bc";

        //_apiUrl: The base URL for the API. Find out what this is for other APIs via the API documentation
        public const string _apiUrlBase = "https://api.projectoxford.ai/vision/v1.0/generateThumbnail";

        public async static Task<Stream> GetImageThumbnail(Stream sourceImage, int height, int width)
        {
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient
                httpClient.BaseAddress = new Uri(_apiUrlBase);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

                //setup data object
                HttpContent content = new StreamContent(sourceImage);
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                // Request parameters
                var uri = $"{_apiUrlBase}?width={width}&height={height}&smartCropping={true}";

                //make request
                var response = await httpClient.PostAsync(uri, content);

                //return stream
                return await response.Content.ReadAsStreamAsync();
            }
        }

    }
}