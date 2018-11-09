using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CFS.CRMtoAX
{
    public class IntegrateAccountScribe : IPlugin
    {
        //IOrganizationService service;
        //IPluginExecutionContext context;
        //Entity currentRecord, postImage;

        //ITracingService tracingObject;

        private void SetCurrentRecord()
        {
            try
            {
                //    if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                //    {
                //        this.currentRecord = context.InputParameters["Target"] as Entity;

                //    }
            }
            catch (Exception ex)
            {
                //tracingObject.Trace(ex.Message + "<--SetCurrentRecord--->" + ex.StackTrace);
                //CreateErrorLog(LogError.LogErrors(ex.Message, ex.StackTrace, null, MethodBase.GetCurrentMethod().Name, this.GetType().Name, null, context.UserId));
            }
        }

        private void SetOrganizationService(IServiceProvider serviceProvider)
        {
            try
            {
                ////context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                ////service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);


                ////tracingObject = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            }
            catch (Exception ex)
            {
                //tracingObject.Trace(ex.Message + "<--SetOrganizationService--->" + ex.StackTrace);
                //CreateErrorLog(LogError.LogErrors(ex.Message, ex.StackTrace, null, MethodBase.GetCurrentMethod().Name, this.GetType().Name, null, Guid.Empty));
            }
        }

        private void SetPostImage()
        {
            try
            {
                //    if (context.PostEntityImages.Contains("postimage"))
                //    {
                //        this.postImage = (Entity)context.PostEntityImages["postimage"];
                //    }
                //    else
                //    {
                //        throw new Exception("No Post Image found");
                //  }
            }
            catch (Exception ex)
            {
                //tracingObject.Trace(ex.Message + "<--SetPostImage--->" + ex.StackTrace);
                //CreateErrorLog(LogError.LogErrors(ex.Message, ex.StackTrace, null, MethodBase.GetCurrentMethod().Name, this.GetType().Name, null, context.UserId));
            }
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            IOrganizationService service;
            IPluginExecutionContext context;
            Entity currentRecord = null;
            Entity postImage; ;

            ITracingService tracingObject;
            try
            {
                tracingObject = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                //this.SetOrganizationService(serviceProvider);
                //this.SetCurrentRecord();
                //this.SetPostImage();

                #region SetOrganizationService

                context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                service = ((IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))).CreateOrganizationService(context.UserId);

                tracingObject = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                #endregion

                #region SetCurrentRecord
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {
                    currentRecord = context.InputParameters["Target"] as Entity;

                }

                #endregion

                #region SetPostImage

                if (context.PostEntityImages.Contains("postimage"))
                {
                    postImage = (Entity)context.PostEntityImages["postimage"];
                }
                else
                {
                    throw new Exception("No Post Image found");
                }
                #endregion



                #region Call Scribe Service to Create Customer in Cloud AX

                //tracingObject.Trace("INSIDE Execute");
                AccountObject2 account = new AccountObject2();

                account.addressNumber = ReadAddress(postImage.GetAttributeValue<Guid>("address1_addressid"), service);

                account.AccountId = currentRecord.Id;
                account.AccountName = postImage.GetAttributeValue<string>("name");
                account.AccountNumber = postImage.GetAttributeValue<string>("accountnumber");
                account.CurrencyName = GetCurrencyISOCode(service, postImage);
                account.AddressName = postImage.GetAttributeValue<string>("address1_name");
                account.AddressType = postImage.FormattedValues.Contains("address1_addresstypecode") ? postImage.FormattedValues["address1_addresstypecode"].ToString() : string.Empty;
                account.Street = postImage.GetAttributeValue<string>("address1_line1");
                account.City = postImage.GetAttributeValue<string>("address1_city");
                account.State = postImage.GetAttributeValue<string>("address1_stateorprovince");
                account.Country = postImage.GetAttributeValue<string>("address1_country");
                account.ZipCode = postImage.GetAttributeValue<string>("address1_postalcode");
                account.Phone = postImage.GetAttributeValue<string>("telephone1");
                account.Email = postImage.GetAttributeValue<string>("emailaddress1");
                account.Website = postImage.GetAttributeValue<string>("websiteurl");
                account.RelationshipType = (postImage.GetAttributeValue<OptionSetValue>("customertypecode") == null) ? 3 : postImage.GetAttributeValue<OptionSetValue>("customertypecode").Value;
                account.CompanyName = postImage.FormattedValues.Contains("cf_companyname") ? postImage.FormattedValues["cf_companyname"].ToString() : "USMF";


                string webAddr = GetAccountIntegrationURL(service); /// this will be the Scribe Endpoint URL
                tracingObject.Trace("Scribe Endpoint URL - " + webAddr);
                
                if (webAddr != string.Empty)
                    SendPOSTRequestToRestAPI(account, tracingObject, webAddr);
                else
                {
                    throw new InvalidPluginExecutionException(OperationStatus.Failed, "Cannot find Scribe Real time Integration URL from Integration configurations. Configuration Name: 'Account Integration Scribe URL'");
                }
                #endregion

            }

            catch (InvalidPluginExecutionException ex)
            {
                //tracingObject.Trace(ex.Message + "<--Execute--->" + ex.StackTrace);
                throw ex;
            }
            catch (TimeoutException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                //tracingObject.Trace(ex.Message + "<--Execute--->" + ex.StackTrace);
                //CreateErrorLog(LogError.LogErrors(ex.Message, ex.StackTrace, currentRecord.LogicalName, MethodBase.GetCurrentMethod().Name, this.GetType().Name, currentRecord.Id.ToString(), context.UserId));
            }
        }

        private string GetAccountIntegrationURL(IOrganizationService service)
        {
            string configName = "Account Integration Scribe URL";

            FilterExpression filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition("cf_key", ConditionOperator.Equal, configName);

            QueryExpression query = new QueryExpression()
            {
                EntityName = "cf_integrationconfiguration",
                Criteria = filter,
                ColumnSet = new ColumnSet("cf_value", "cf_key", "cf_description")

            };

            EntityCollection configurations = service.RetrieveMultiple(query);

            if (configurations.Entities.Count > 0 && configurations[0].Contains("cf_value"))
                return configurations[0].GetAttributeValue<string>("cf_value");

            return string.Empty;

        }

        private string ReadAddress(Guid AddressId, IOrganizationService service)
        {
            string fetchXml = string.Empty;
            EntityCollection addresses = null;
            fetchXml = @"<fetch mapping='logical' version='1.0' output-format='xml-platform' distinct='false'>
                          <entity name='customeraddress'>
                            <attribute name='name' />
                            <attribute name='line1' />
                            <attribute name='city' />
                            <attribute name='postalcode' />
                            <attribute name='telephone1' />
                            <attribute name='customeraddressid' />
                            <attribute name='addressnumber' />
                            <order descending='false' attribute='name' />
                            <filter type='and'>
                              <condition value='" + AddressId + @"' attribute='customeraddressid' uitype='customeraddress' uiname='Secondary Address' operator='eq' />
                            </filter>
                          </entity>
                        </fetch>";

            addresses = service.RetrieveMultiple(new FetchExpression(fetchXml));

            return addresses[0].Attributes["addressnumber"].ToString();
        }
        //private string ParseString(string data) {
        //    if(Str)
        //}
        private string GetCurrencyISOCode(IOrganizationService service, Entity postImage)
        {
            string currencyCode = "USD";
            FilterExpression filter = new FilterExpression(LogicalOperator.And);
            filter.AddCondition("transactioncurrencyid", ConditionOperator.Equal, postImage.GetAttributeValue<EntityReference>("transactioncurrencyid").Id);

            QueryExpression query = new QueryExpression()
            {
                EntityName = "transactioncurrency",
                ColumnSet = new ColumnSet(true),
                Criteria = filter
            };

            EntityCollection currencies = service.RetrieveMultiple(query);
            if (currencies.Entities.Count > 0)
                currencyCode = currencies.Entities[0].GetAttributeValue<string>("isocurrencycode");

            return currencyCode;

        }

        void SendPOSTRequestToRestAPI(AccountObject2 account, ITracingService tracingObject, string webAddr)
        {

            tracingObject.Trace("Inside: " + "SendPOSTRequestToRestAPI");
            
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
            httpWebRequest.KeepAlive = false;
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Method = "POST";
            tracingObject.Trace("HTTP Request URL: " + httpWebRequest.RequestUri);

            try
            {
                tracingObject.Trace("Request Sent: " + "SendPOSTRequestToRestAPI");
                //tracingObject.Trace("INSIDE SendPOSTRequestToRestAPI" );
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = "{\"Name\":\"" + account.AccountName + "\",\"addStreet\":\"" + account.Street + "\",\"AddRoles\":\"" + account.AddressType + "\",\"Addstate\":\"" + account.State + "\",\"AddCity\":\"" + account.City + "\",\"AddZipCode\":\"" + account.ZipCode + "\",\"AddCountry\":\"" + account.Country + "\",\"Currency\":\"" + account.CurrencyName + "\",\"CustGroupId\":\"" + account.CustGrpId + "\",\"AccNo\":\"" + account.AccountNumber + "\",\"AddrName\":\"" + account.AddressName + "\",\"AccId\":\"" + account.AccountId + "\",\"RelType\":\"" + account.RelationshipType + "\",\"Phone\":\"" + account.Phone + "\",\"Email\":\"" + account.Email + "\",\"Website\":\"" + account.Website + "\",\"CompName\":\"" + account.CompanyName + "\",\"AddressNumber\":\"" + account.addressNumber + "\" }";
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AccountObject2));
                    MemoryStream stream = new MemoryStream();
                    serializer.WriteObject(stream, account);
                    stream.Position = 0;
                    StreamReader streamReader = new StreamReader(stream);

                    //string json = streamReader.ReadToEnd();

                    tracingObject.Trace("Request Data: " + json);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    //tracingObject.Trace("JSON:- " + json);
                }


                //DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(ResultSet));
                //ResultSet res = (ResultSet)serializer.ReadObject(httpWebRequest.GetResponse().GetResponseStream());
                //throw new InvalidPluginExecutionException(OperationStatus.Failed, "status in GP:" + res.data[0].status);
                //if (res.data.Count > 0 && res.data[0].status != null && res.data[0].status == "1")
                //    return true;

                var data = httpWebRequest.GetResponse().GetResponseStream();
                data.Close();
                data.Dispose();
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(OperationStatus.Failed, ex.Message + " " + ex.InnerException);
            }
            //tracingObject.Trace("Request Send");
        }

    }


    [DataContract]
    public class AccountObject2
    {
        [DataMember(Name = "AccId")]
        public Guid AccountId { get; set; }

        [DataMember(Name = "Name")]
        public string AccountName { get; set; }

        [DataMember(Name = "AccNo")]
        public string AccountNumber { get; set; }

        [DataMember(Name = "AddrName")]
        public string AddressName { get; set; }

        [DataMember(Name = "AddRoles")]
        public string AddressType { get; set; }

        [DataMember(Name = "addStreet")]
        public string Street { get; set; }

        [DataMember(Name = "AddCity")]
        public string City { get; set; }

        [DataMember(Name = "Addstate")]
        public string State { get; set; }

        [DataMember(Name = "AddCountry")]
        public string Country { get; set; }

        [DataMember(Name = "AddZipCode")]
        public string ZipCode { get; set; }

        [DataMember(Name = "Currency")]
        public string CurrencyName { get; set; }

        [DataMember(Name = "Phone")]
        public string Phone { get; set; }

        [DataMember(Name = "Email")]
        public string Email { get; set; }

        [DataMember(Name = "Website")]
        public string Website { get; set; }

        [DataMember(Name = "RelType")]
        public int RelationshipType { get; set; }

        [DataMember(Name = "CompName")]
        public string CompanyName { get; set; }

        public string addressNumber { get; set; }

        private string custGrpId = "90";

        [DataMember(Name = "CustGroupId")]
        public string CustGrpId
        {
            get
            {
                custGrpId = this.RelationshipType == 11 ? "40" : "90";
                return custGrpId;
            }
            set { }

        }

    }
}
