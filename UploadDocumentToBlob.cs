/*
MIT License
Copyright (c) 2016 somesh2207
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using AzureBlobStorage;
using Microsoft.Xrm.Sdk;
using System;

namespace CRMToAzureBlob
{
    public class UploadDocumentToBlob : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;

        public UploadDocumentToBlob(string unsecureConfig, string secureConfig)
        {
            _secureConfig = secureConfig;
            _unsecureConfig = unsecureConfig;
        }
        #endregion
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            try
            {
                Entity entity = (Entity)context.InputParameters["Target"];
                string documentBlobURL = string.Empty;

                //// Optional condition to migrate attachments related to particular entity.
                //// If you want to migrate attachments for all entities, remove this CONDITION
                if (entity.Contains("isdocument") && entity.GetAttributeValue<bool>("isdocument") == true &&
                    entity.GetAttributeValue<string>("objecttypecode") == "account")
                {
                    string storageAccount = "<storageaccountname>";
                    string filename = entity.GetAttributeValue<string>("filename");
                    string containerName = "<blobcontainername>";
                    string storageKey = "<blobstorage_accesskey>";

                    #region Read File
                    string text = entity.GetAttributeValue<string>("documentbody");

                    #endregion


                    BlobHelper blobHelper = new BlobHelper(storageAccount, storageKey);

                    bool isUploadSuccess = blobHelper.PutBlob(containerName, filename, text);

                    //// Once blob upload is Success, get the Azure blob download-able URL of the uploaded File
                    if (isUploadSuccess)
                        documentBlobURL = string.Format("https://{0}.blob.core.windows.net/{1}/{2}", storageAccount, containerName, filename);

                }

                //// UPDATE the blob URL link in Account Description field. You can use it in whatever way as REQUIRED
                if (!string.IsNullOrEmpty(documentBlobURL))
                {
                    Entity account = new Entity("account");
                    account.Id = entity.Id;
                    account.Attributes["description"] = documentBlobURL;

                    service.Update(account);

                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}
