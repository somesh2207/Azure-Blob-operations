using System;
using System.Globalization;
using System.IO;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureBlob
{
    class AzureBlobSAS
    {
        static void Main(string[] args)
        {

            //Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            //Create the blob client object.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            string containerName = "containername";

            //Get a reference to a container to use for the sample code, and create it if it does not exist.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            //container.CreateIfNotExists();

            //Insert calls to the methods created below here...

            Console.WriteLine("Blob Container: " + containerName + "retrieved successfully. Press any key to continue... ");
            Console.ReadLine();

            #region Get SAS for Blob URL

            string blobname = "BlobFileName.extension";
            string accesskey = GetBlobSasUri(blobname, container);

            #endregion

            #region Create and set the Values in Text document

            Console.WriteLine(FormatTheSASInfo(accesskey, blobname));
            Console.WriteLine("Press Any Key to Exit...");

            #endregion
            //Require user input before closing the console window.

            Console.ReadLine();
        }

        private static string FormatTheSASInfo(string accesskey, string blobname)
        {
            // Compose a string that consists of three lines.
            string dtformat = "dd MMM yyyy hh:mm tt";            

            string lineExc = "DateTime:" + DateTime.Now.ToString(dtformat) +
                Environment.NewLine + "Name: " + blobname +
                Environment.NewLine + "Access Key: " + accesskey;

            return lineExc;
        }

        static string GetBlobSasUri(string blobName, CloudBlobContainer container)
        {
            //Get a reference to a blob within the container.
            CloudBlockBlob blob = container.GetBlockBlobReference(blobName);

            //Set the expiry time and permissions for the blob.
            //In this case, the start time is specified as a few minutes in the past, to mitigate clock skew.
            //The shared access signature will be valid immediately.
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
            sasConstraints.SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5);
            sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddYears(1);
            sasConstraints.Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.List;

            //Generate the shared access signature on the blob, setting the constraints directly on the signature.
            string sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

            //Return the URI string for the container, including the SAS token.
            return blob.Uri + sasBlobToken;
        }
    }
}
