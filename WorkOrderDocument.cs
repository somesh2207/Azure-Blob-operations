using Microsoft.Xrm.Sdk;
using System;
using AzureBlobStorage;
using System.Collections.Generic;

namespace AnnotationPlugin
{
    public class WorkOrderDocument : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;

        public WorkOrderDocument(string unsecureConfig, string secureConfig)
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
                if (context.MessageName.ToLowerInvariant().Equals("create"))
                {
                    if (entity.Contains("cf_workorder") && entity.Contains("cf_documentlink"))
                    {
                        #region Decode the link
                        EntityReference workOrder = entity.GetAttributeValue<EntityReference>("cf_workorder");
                        
                        #endregion

                        #region Connect and fetch the data from Blob storage
                        // Replace the below values with actual details from your Azure Blob storage
                        string storageAccount = "blobstorageaccountname";
                        string filename = "filenamehere"; // testdocument.pdf
                        string containerName = "containernameHere"; //documents
                        string storageKey = "primaryaccesskiyeofazureblobstorageaccount";


                        BlobHelper blobHelper = new BlobHelper(storageAccount, storageKey);

                        KeyValuePair<byte[], string> data = blobHelper.GetBlobResponse(containerName, filename);

                        byte[] body = data.Key;
                        string contentType = data.Value;
                        #endregion

                        #region Create Annotation in CRM
                        string encodedData = System.Convert.ToBase64String(body);
                        Entity Annotation = new Entity("annotation");
                        Annotation.Attributes["objectid"] = new EntityReference(workOrder.LogicalName, workOrder.Id);
                        Annotation.Attributes["objecttypecode"] = workOrder.LogicalName;
                        Annotation.Attributes["subject"] = "Document from  AX Integration";
                        Annotation.Attributes["documentbody"] = encodedData;
                        Annotation.Attributes["mimetype"] = contentType;
                        Annotation.Attributes["notetext"] = "REST API - Sample document from  AX.";
                        Annotation.Attributes["filename"] = entity.GetAttributeValue<string>("cf_name");

                        Guid annotation = service.Create(Annotation);
                        #endregion
                    }
                }
                else { return; }

            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}
