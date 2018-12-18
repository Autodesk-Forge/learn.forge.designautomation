/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using Autodesk.Forge;
using Autodesk.Forge.DesignAutomation.v3;
using Autodesk.Forge.Model;
using Autodesk.Forge.Model.DesignAutomation.v3;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ActivitiesApi = Autodesk.Forge.DesignAutomation.v3.ActivitiesApi;
using Activity = Autodesk.Forge.Model.DesignAutomation.v3.Activity;
using EnginesApi = Autodesk.Forge.DesignAutomation.v3.EnginesApi;
using WorkItem = Autodesk.Forge.Model.DesignAutomation.v3.WorkItem;
using WorkItemsApi = Autodesk.Forge.DesignAutomation.v3.WorkItemsApi;

namespace forgeSample.Controllers
{
    [ApiController]
    public class DesignAutomationController : ControllerBase
    {
        // Used to access the application folder (temp location for files & bundles)
        private IHostingEnvironment _env;
        // used to access the SignalR Hub
        private IHubContext<DesignAutomationHub> _hubContext;
        // Constructor, where env and hubContext are specified
        public DesignAutomationController(IHostingEnvironment env, IHubContext<DesignAutomationHub> hubContext) { _env = env; _hubContext = hubContext; }
        // Local folder for bundles
        public string LocalBundlesFolder { get { return Path.Combine(_env.WebRootPath, "bundles"); } }
        /// Prefix for AppBundles and Activities
        public static string NickName { get { return OAuthController.GetAppSetting("FORGE_CLIENT_ID"); } }
        /// Alias for the app (e.g. DEV, STG, PROD). This value may come from an environment variable
        public static string Alias { get { return "dev"; } }

        /// <summary>
        /// Get all Activities defined for this account
        /// </summary>
        [HttpGet]
        [Route("api/forge/designautomation/activities")]
        public async Task<List<string>> GetDefinedActivities()
        {
            dynamic oauth = await OAuthController.GetInternalAsync();

            // define Activities API
            ActivitiesApi activitiesApi = new ActivitiesApi();
            activitiesApi.Configuration.AccessToken = oauth.access_token; ;

            // filter list of 
            PageString activities = await activitiesApi.ActivitiesGetItemsAsync();
            List<string> definedActivities = new List<string>();
            foreach (string activity in activities.Data)
                if (activity.StartsWith(NickName) && activity.IndexOf("$LATEST") == -1)
                    definedActivities.Add(activity.Replace(NickName + ".", String.Empty));

            return definedActivities;
        }

        /// <summary>
        /// Define a new activity
        /// </summary>
        [HttpPost]
        [Route("api/forge/designautomation/activities")]
        public async Task<IActionResult> CreateActivity([FromBody]JObject activitySpecs)
        {
            // basic input validation
            string zipFileName = activitySpecs["zipFileName"].Value<string>();
            string engineName = activitySpecs["engine"].Value<string>();

            // define Activities API
            dynamic oauth = await OAuthController.GetInternalAsync();
            ActivitiesApi activitiesApi = new ActivitiesApi();
            activitiesApi.Configuration.AccessToken = oauth.access_token;

            // standard name for this sample
            string appBundleName = zipFileName + "AppBundle";
            string activityName = zipFileName + "Activity";

            // 
            PageString activities = await activitiesApi.ActivitiesGetItemsAsync();
            string qualifiedActivityId = string.Format("{0}.{1}+{2}", NickName, activityName, Alias);
            if (!activities.Data.Contains(qualifiedActivityId))
            {
                // define the activity
                // ToDo: parametrize for different engines...
                dynamic engineAttributes = EngineAttributes(engineName);
                string commandLine = string.Format(@"$(engine.path)\\{0} /i $(args[inputFile].path) /al $(appbundles[{1}].path)", engineAttributes.executable, appBundleName);
                ModelParameter inputFile = new ModelParameter(false, false, ModelParameter.VerbEnum.Get, "input file", true, "$(inputFile)");
                ModelParameter inputJson = new ModelParameter(false, false, ModelParameter.VerbEnum.Get, "input json", false, "params.json");
                ModelParameter outputFile = new ModelParameter(false, false, ModelParameter.VerbEnum.Put, "output file", true, "outputFile." + engineAttributes.extension);
                Activity activitySpec = new Activity(
                  new List<string>() { commandLine },
                  new Dictionary<string, ModelParameter>() {
                    { "inputFile", inputFile },
                    { "inputJson", inputJson },
                    { "outputFile", outputFile }
                  },
                  engineName, new List<string>() { string.Format("{0}.{1}+{2}", NickName, appBundleName, Alias) }, null,
                  string.Format("Description for {0}", activityName), null, activityName);
                Activity newActivity = await activitiesApi.ActivitiesCreateItemAsync(activitySpec);

                // specify the alias for this Activity
                Alias aliasSpec = new Alias(1, null, Alias);
                Alias newAlias = await activitiesApi.ActivitiesCreateAliasAsync(activityName, aliasSpec);

                return Ok(new { Activity = qualifiedActivityId });
            }

            // as this activity points to a AppBundle "dev" alias (which points to the last version of the bundle),
            // there is no need to update it (for this sample), but this may be extended for different contexts
            return Ok(new { Activity = "Activity already defined" });
        }

        /// <summary>
        /// Helps identify the engine
        /// </summary>
        private dynamic EngineAttributes(string engine)
        {
            if (engine.Contains("3dsMax")) return new { executable = "3dsmaxbatch.exe", extension = "3ds" };
            if (engine.Contains("AutoCAD")) return new { executable = "accoreconsole.exe", extension = "dwg" };
            if (engine.Contains("Inventor")) return new { executable = "InventorCoreConsole.exe", extension = "ipt" };
            if (engine.Contains("Revit")) return new { executable = "revitcoreconsole.exe", extension = "rvt" };
            throw new Exception("Invalid engine");
        }

        /// <summary>
        /// Define a new activity
        /// </summary>
        [HttpPost]
        [Route("api/forge/designautomation/appbundles")]
        public async Task<IActionResult> CreateAppBundle([FromBody]JObject appBundleSpecs)
        {
            // basic input validation
            string zipFileName = appBundleSpecs["zipFileName"].Value<string>();
            string engineName = appBundleSpecs["engine"].Value<string>();

            // standard name for this sample
            string appBundleName = zipFileName + "AppBundle";

            // check if ZIP with bundle is here
            string packageZipPath = Path.Combine(LocalBundlesFolder, zipFileName + ".zip");
            if (!System.IO.File.Exists(packageZipPath)) throw new Exception("Appbundle not found at " + packageZipPath);

            // define Activities API
            dynamic oauth = await OAuthController.GetInternalAsync();
            AppBundlesApi appBundlesApi = new AppBundlesApi();
            appBundlesApi.Configuration.AccessToken = oauth.access_token;

            // get defined app bundles
            PageString appBundles = await appBundlesApi.AppBundlesGetItemsAsync();

            // check if app bundle is already define
            dynamic newAppVersion;
            string qualifiedAppBundleId = string.Format("{0}.{1}+{2}", NickName, appBundleName, Alias);
            if (!appBundles.Data.Contains(qualifiedAppBundleId))
            {
                // create an appbundle (version 1)
                AppBundle appBundleSpec = new AppBundle(appBundleName, null, engineName, null, null, string.Format("Description for {0}", appBundleName), null, appBundleName);
                newAppVersion = await appBundlesApi.AppBundlesCreateItemAsync(appBundleSpec);
                if (newAppVersion == null) throw new Exception("Cannot create new app");

                // create alias pointing to v1
                Alias aliasSpec = new Alias(1, null, Alias);
                Alias newAlias = await appBundlesApi.AppBundlesCreateAliasAsync(appBundleName, aliasSpec);
            }
            else
            {
                // create new version
                AppBundle appBundleSpec = new AppBundle(null, null, engineName, null, null, appBundleName, null, null);
                newAppVersion = await appBundlesApi.AppBundlesCreateItemVersionAsync(appBundleName, appBundleSpec);
                if (newAppVersion == null) throw new Exception("Cannot create new version");

                // update alias pointing to v+1
                Alias aliasSpec = new Alias(newAppVersion.Version, null, null);
                Alias newAlias = await appBundlesApi.AppBundlesModifyAliasAsync(appBundleName, Alias, aliasSpec);
            }

            // upload the zip with .bundle
            RestClient uploadClient = new RestClient(newAppVersion.UploadParameters.EndpointURL);
            RestRequest request = new RestRequest(string.Empty, Method.POST);
            request.AlwaysMultipartFormData = true;
            foreach (KeyValuePair<string, object> x in newAppVersion.UploadParameters.FormData) request.AddParameter(x.Key, x.Value);
            request.AddFile("file", packageZipPath);
            request.AddHeader("Cache-Control", "no-cache");
            await uploadClient.ExecuteTaskAsync(request);

            return Ok(new { AppBundle = qualifiedAppBundleId, Version = newAppVersion.Version });
        }

        /// <summary>
        /// Input for StartWorkitem
        /// </summary>
        public class StartWorkitemInput
        {
            public IFormFile inputFile { get; set; }
            public string data { get; set; }
        }

        /// <summary>
        /// Start a new workitem
        /// </summary>
        [HttpPost]
        [Route("api/forge/designautomation/workitems")]
        public async Task<IActionResult> StartWorkitem([FromForm]StartWorkitemInput input)
        {
            // basic input validation
            JObject workItemData = JObject.Parse(input.data);
            string widthParam = workItemData["width"].Value<string>();
            string heigthParam = workItemData["height"].Value<string>();
            string activityName = string.Format("{0}.{1}", NickName, workItemData["activityName"].Value<string>());
            string browerConnectionId = workItemData["browerConnectionId"].Value<string>();

            // save the file on the server
            var fileSavePath = Path.Combine(_env.ContentRootPath, input.inputFile.FileName);
            using (var stream = new FileStream(fileSavePath, FileMode.Create)) await input.inputFile.CopyToAsync(stream);

            // OAuth token
            dynamic oauth = await OAuthController.GetInternalAsync();

            // upload file to OSS Bucket
            // 1. ensure bucket existis
            string bucketKey = NickName.ToLower() + "_designautomation";
            BucketsApi buckets = new BucketsApi();
            buckets.Configuration.AccessToken = oauth.access_token;
            try
            {
                PostBucketsPayload bucketPayload = new PostBucketsPayload(bucketKey, null, PostBucketsPayload.PolicyKeyEnum.Transient);
                await buckets.CreateBucketAsync(bucketPayload, "US");
            }
            catch { }; // in case bucket already exists
            // 2. upload inputFile
            string inputFileNameOSS = string.Format("{0}_input_{1}", DateTime.Now.ToString("yyyyMMddhhmmss"), input.inputFile.FileName); // avoid overriding
            ObjectsApi objects = new ObjectsApi();
            objects.Configuration.AccessToken = oauth.access_token;
            using (StreamReader streamReader = new StreamReader(fileSavePath))
                await objects.UploadObjectAsync(bucketKey, inputFileNameOSS, (int)streamReader.BaseStream.Length, streamReader.BaseStream, "application/octet-stream");
            System.IO.File.Delete(fileSavePath);// delete server copy

            // prepare workitem arguments
            // 1. input file
            JObject inputFileArgument = new JObject
            {
              new JProperty("url", string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketKey, inputFileNameOSS)),
              new JProperty("headers",
              new JObject{
                new JProperty("Authorization", "Bearer " + oauth.access_token)
              })
            };
            // 2. input json
            dynamic inputJson = new JObject();
            inputJson.Width = widthParam;
            inputJson.Height = heigthParam;
            JObject inputJsonArgument = new JObject { new JProperty("url", "data:application/json, " + ((JObject)inputJson).ToString(Formatting.None).Replace("\"", "'")) }; // ToDo: need to improve this
            // 3. output file
            string outputFileNameOSS = string.Format("{0}_output_{1}", DateTime.Now.ToString("yyyyMMddhhmmss"), input.inputFile.FileName); // avoid overriding
            JObject outputFileArgument = new JObject
            {
              new JProperty("verb", "PUT"),
              new JProperty("url", string.Format("https://developer.api.autodesk.com/oss/v2/buckets/{0}/objects/{1}", bucketKey, outputFileNameOSS)),
              new JProperty("headers",
              new JObject{
                new JProperty("Authorization", "Bearer " + oauth.access_token)
              })
            };

            // prepare & submit workitem
            string callbackUrl = string.Format("{0}/api/forge/callback/designautomation?id={1}", OAuthController.GetAppSetting("FORGE_WEBHOOK_CALLBACK_HOST"), browerConnectionId);
            WorkItem workItemSpec = new WorkItem(
                      null, activityName,
                      new Dictionary<string, JObject>()
                      {
                        { "inputFile", inputFileArgument },
                        { "inputJson",  inputJsonArgument},
                        { "outputFile", outputFileArgument },
                        //{ "onProgress", new JObject { new JProperty("verb", "POST"), new JProperty("url", callbackUrl) }},
                        { "onComplete", new JObject { new JProperty("verb", "POST"), new JProperty("url", callbackUrl) }}
                      },
                      null);
            WorkItemsApi workItemApi = new WorkItemsApi();
            workItemApi.Configuration.AccessToken = oauth.access_token; ;
            WorkItemStatus newWorkItem = await workItemApi.WorkItemsCreateWorkItemsAsync(null, null, workItemSpec);

            return Ok(new { WorkItemId = newWorkItem.Id });
        }

        /// <summary>
        /// Callback from Design Automation Workitem (onProgress or onComplete)
        /// </summary>
        [HttpPost]
        [Route("/api/forge/callback/designautomation")]
        public async Task<IActionResult> OnCallback(string id, [FromBody]dynamic body)
        {
            try
            {
                // your webhook should return immediately! we can use Hangfire to schedule a job
                JObject bodyJson = JObject.Parse((string)body.ToString());
                await _hubContext.Clients.Client(id).SendAsync("onComplete", bodyJson.ToString());

                var client = new RestClient(bodyJson["reportUrl"].Value<string>());
                var request = new RestRequest(string.Empty);

                Console.WriteLine(id);

                byte[] bs = client.DownloadData(request);
                string report = System.Text.Encoding.Default.GetString(bs);
                await _hubContext.Clients.Client(id).SendAsync("onComplete", report);
            }
            catch (Exception e) { }

            // ALWAYS return ok (200)
            return Ok();
        }

        /// <summary>
        /// Return a list of available engines
        /// </summary>
        [HttpGet]
        [Route("api/forge/designautomation/engines")]
        public async Task<List<string>> GetAvailableEngines()
        {
            dynamic oauth = await OAuthController.GetInternalAsync();

            // define Engines API
            EnginesApi enginesApi = new EnginesApi();
            enginesApi.Configuration.AccessToken = oauth.access_token;
            PageString engines = await enginesApi.EnginesGetItemsAsync();
            engines.Data.Sort();

            return engines.Data; // return list of engines
        }

        /// <summary>
        /// Clear the accounts (for debugging purpouses)
        /// </summary>
        [HttpDelete]
        [Route("api/forge/designautomation/account")]
        public async Task<IActionResult> ClearAccount()
        {
            if (!_env.IsDevelopment()) return BadRequest(); // disable when published :-)

            dynamic oauth = await OAuthController.GetInternalAsync();

            // define the account api (ForgeApps)
            Autodesk.Forge.DesignAutomation.v3.ForgeAppsApi forgeAppApi = new ForgeAppsApi();
            forgeAppApi.Configuration.AccessToken = oauth.access_token;

            // clear account
            await forgeAppApi.ForgeAppsDeleteUserAsync("me");
            return Ok();
        }

        /// <summary>
        /// Names of app bundles on this project
        /// </summary>
        [HttpGet]
        [Route("api/appbundles")]
        public string[] GetLocalBundles()
        {
            // this folder is placed under the public folder, which may expose the bundles
            // but it was defined this way so it be published on most hosts easily
            return Directory.GetFiles(LocalBundlesFolder, "*.zip").Select(Path.GetFileNameWithoutExtension).ToArray();
        }
    }

    /// <summary>
    /// Class uses for SignalR
    /// </summary>
    public class DesignAutomationHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public string GetConnectionId() { return Context.ConnectionId; }
    }
}