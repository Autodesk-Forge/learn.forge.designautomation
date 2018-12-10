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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Autodesk.Forge;
using Autodesk.Forge.DesignAutomation.v3;
using Autodesk.Forge.Model;
using Autodesk.Forge.Model.DesignAutomation.v3;
using EnginesApi = Autodesk.Forge.DesignAutomation.v3.EnginesApi;
using ActivitiesApi = Autodesk.Forge.DesignAutomation.v3.ActivitiesApi;
using Activity = Autodesk.Forge.Model.DesignAutomation.v3.Activity;
using WorkItem = Autodesk.Forge.Model.DesignAutomation.v3.WorkItem;
using WorkItemsApi = Autodesk.Forge.DesignAutomation.v3.WorkItemsApi;
using Newtonsoft.Json.Linq;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using RestSharp;

namespace forgeSample.Controllers
{
    [ApiController]
    public class DesignAutomationController : ControllerBase
    {
        private IHostingEnvironment _env;
        public DesignAutomationController(IHostingEnvironment env) { _env = env; }

        public string LocalBundlesFolder { get { return Path.Combine(_env.WebRootPath, "bundles"); } }

        /// <summary>
        /// Prefix for AppBundles and Activities
        /// </summary>
        public static string NickName { get { return OAuthController.GetAppSetting("FORGE_CLIENT_ID"); } }

        /// <summary>
        /// Alias for the app (e.g. DEV, STG, PROD)
        /// This value may come from an environment variable
        /// </summary>
        public static string Alias { get { return "dev"; } }

        /// <summary>
        /// Get all Activities defined for this account
        /// </summary>
        /// <returns></returns>
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
        /// <returns></returns>
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
                string commandLine = string.Format(@"$(engine.path)\\{0} /i $(args[inputFile].path) /al $(appbundles[{1}].path)", Executable(engineName), appBundleName);
                ModelParameter rvtFile = new ModelParameter(false, false, ModelParameter.VerbEnum.Get, "input file", true, "$(inputFile)");
                ModelParameter parameterInput = new ModelParameter(false, false, ModelParameter.VerbEnum.Get, "input json", false, "params.json");
                ModelParameter result = new ModelParameter(false, false, ModelParameter.VerbEnum.Put, "output file", true, "outputFile.rvt");
                Activity activitySpec = new Activity(
                  new List<string>() { commandLine },
                  new Dictionary<string, ModelParameter>() {
                    { "inputFile", rvtFile },
                    { "inputJson", parameterInput },
                    { "outputFile", result }
                  },
                  engineName,
                  new List<string>() { string.Format("{0}.{1}+{2}", NickName, appBundleName, Alias) },
                  null,
                  activityName,
                  null,
                  activityName);
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

        private string Executable(string engine)
        {
            if (engine.Contains("3dsMax")) return "3dsmaxbatch.exe";
            if (engine.Contains("AutoCAD")) return "accoreconsole.exe";
            if (engine.Contains("Inventor")) return "InventorCoreConsole.exe";
            if (engine.Contains("Revit")) return "revitcoreconsole.exe";          
            throw new Exception("Invalid engine");
        }

        /// <summary>
        /// Define a new activity
        /// </summary>
        /// <returns></returns>
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
                AppBundle appBundleSpec = new AppBundle(appBundleName, null, engineName, null, null, appBundleName, null, appBundleName);
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
            foreach (KeyValuePair<string, object> x in newAppVersion.UploadParameters.FormData)
                request.AddParameter(x.Key, x.Value);
            request.AddFile("file", packageZipPath);
            request.AddHeader("Cache-Control", "no-cache");
            await uploadClient.ExecuteTaskAsync(request);

            return Ok(new { AppBundle = qualifiedAppBundleId, Version = newAppVersion.Version });
        }

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

        [HttpDelete]
        [Route("api/forge/designautomation/account")]
        public async Task<IActionResult> ClearAccount()
        {
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
        /// <returns></returns>
        [HttpGet]
        [Route("api/appbundles")]
        public string[] GetLocalBundles()
        {
            // this folder is placed under the public folder, which may expose the bundles
            // but it was defined this way so it be published on most hosts easily
            return Directory.GetFiles(LocalBundlesFolder, "*.zip").Select(Path.GetFileNameWithoutExtension).ToArray();
        }
    }
}