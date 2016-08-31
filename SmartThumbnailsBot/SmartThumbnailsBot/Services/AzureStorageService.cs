using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.IO;

namespace SmartThumbnailsBot.Services
{
    public static class AzureStorageService
    {
        public static string Upload(byte[] file, string filename)
        {
            if (file.Length > 0)
            {
                var container = GetBlobContainer();
                
                CloudBlockBlob blockBlob = container.GetBlockBlobReference(filename);
                
                using (var ms = new MemoryStream(file, false))
                {
                    blockBlob.UploadFromStream(ms);
                }

                return blockBlob.StorageUri.PrimaryUri.AbsoluteUri;
            }
            else
            {
                return string.Empty;
            }
        }

        public static void Delete(string filename)
        {
            var container = GetBlobContainer();

            // Retrieve reference to a blob named "myblob.txt".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(filename);

            // Delete the blob.
            blockBlob.DeleteIfExists();
        }

        public static CloudBlobContainer GetBlobContainer()
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString");

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(CloudConfigurationManager.GetSetting("StorageBlobContainer");

            return container;
        }
    }
}