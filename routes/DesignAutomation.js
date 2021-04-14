const _path = require('path');
const _fs = require('fs');
const _url = require('url');
const express = require('express');
//const http = require('http');
const http = require('https');
const formdata = require('form-data');
const bodyParser = require('body-parser');
const multer = require('multer');
const router = express.Router();
const { getClient } = require('./common/oauth');
const config = require('../config');
const dav3 = require('autodesk.forge.designautomation');
const ForgeAPI = require('forge-apis');

router.use(bodyParser.json());

// Middleware for obtaining a token for each request.
router.use(async (req, res, next) => {
    req.oauth_client = await getClient(/*config.scopes.internal*/);
    req.oauth_token = req.oauth_client.getCredentials();
    next();
});

// Static instance of the DA API
let dav3Instance = null;

class Utils {

    static async Instance () {
        if (dav3Instance === null) {
            // Here it is ok to not await since we awaited in the call router.use()
            dav3Instance = new dav3.AutodeskForgeDesignAutomationClient(config.client);
            let FetchRefresh = async (data) => { // data is undefined in a fetch, but contains the old credentials in a refresh
                let client = await getClient();
                let credentials = client.getCredentials();
                // The line below is for testing
                //credentials.expires_in = 30; credentials.expires_at = new Date(Date.now() + credentials.expires_in * 1000);
                return (credentials);
            };
            dav3Instance.authManager.authentications['2-legged'].fetchToken = FetchRefresh;
            dav3Instance.authManager.authentications['2-legged'].refreshToken = FetchRefresh;
        }
        return (dav3Instance);
    }

    /// <summary>
    /// Returns the directory where bindles are stored on the local machine.
    /// </summary>
    static get LocalBundlesFolder () {
        return (_path.resolve(_path.join(__dirname, '../', 'bundles')));
    }

    /// <summary>
    /// Prefix for AppBundles and Activities
    /// </summary>
    static get NickName () {
        return (config.credentials.client_id);
    }

    /// <summary>
    /// Alias for the app (e.g. DEV, STG, PROD). This value may come from an environment variable
    /// </summary>
    static get Alias () {
        return ('dev');
    }

    /// <summary>
    /// Search files in a folder and filter them.
    /// </summary>
    static async findFiles (dir, filter) {
        return (new Promise((fulfill, reject) => {
            _fs.readdir(dir, (err, files) => {
                if (err)
                    return (reject(err));
                if (filter !== undefined && typeof filter === 'string')
                    files = files.filter((file) => {
                        return (_path.extname(file) === filter);
                    });
                else if (filter !== undefined && typeof filter === 'object')
                    files = files.filter((file) => {
                        return (filter.test(file));
                    });
                fulfill(files);
            });
        }));
    }

    /// <summary>
    /// Create a new DAv3 client/API with default settings
    /// </summary>
    static async dav3API (oauth2) {
        // There is 2 alternatives to setup an API instance, providing the access_token directly
        // let apiClient2 = new dav3.AutodeskForgeDesignAutomationClient(/*config.client*/);
        // apiClient2.authManager.authentications['2-legged'].accessToken = oauth2.access_token;
        //return (new dav3.AutodeskForgeDesignAutomationApi(apiClient));

        // Or use the Auto-Refresh feature
        let apiClient = await Utils.Instance();
        return (new dav3.AutodeskForgeDesignAutomationApi(apiClient));
    }

    /// <summary>
    /// Helps identify the engine
    /// </summary>
    static EngineAttributes (engine) {
        if (engine.includes('3dsMax'))
            return ({
                commandLine: '$(engine.path)\\3dsmaxbatch.exe -sceneFile "$(args[inputFile].path)" "$(settings[script].path)"',
                extension: 'max',
                script: "da = dotNetClass(\'Autodesk.Forge.Sample.DesignAutomation.Max.RuntimeExecute\')\nda.ModifyWindowWidthHeight()\n"
            });
        if (engine.includes('AutoCAD'))
            return ({
                commandLine: '$(engine.path)\\accoreconsole.exe /i "$(args[inputFile].path)" /al "$(appbundles[{0}].path)" /s "$(settings[script].path)"',
                extension: 'dwg',
                script: "UpdateParam\n"
            });
        if (engine.includes('Inventor'))
            return ({
                commandLine: '$(engine.path)\\InventorCoreConsole.exe /i "$(args[inputFile].path)" /al "$(appbundles[{0}].path)"',
                extension: 'ipt',
                script: ''
            });
        if (engine.includes('Revit'))
            return ({
                commandLine: '$(engine.path)\\revitcoreconsole.exe /i "$(args[inputFile].path)" /al "$(appbundles[{0}].path)"',
                extension: 'rvt',
                script: ''
            });

        throw new Error('Invalid engine');
    }

    static FormDataLength (form) {
        return (new Promise((fulfill, reject) => {
            form.getLength((err, length) => {
                if (err)
                    return (reject(err));
                fulfill(length);
            });
        }));
    }

    /// <summary>
    /// Upload a file
    /// </summary>
    static uploadFormDataWithFile (filepath, endpoint, params = null) {
        return (new Promise(async (fulfill, reject) => {
            const fileStream = _fs.createReadStream(filepath);

            const form = new formdata();
            if (params) {
                const keys = Object.keys(params);
                for (let i = 0; i < keys.length; i++)
                    form.append(keys[i], params[keys[i]]);
            }
            form.append('file', fileStream);

            let headers = form.getHeaders();
            headers['Cache-Control'] = 'no-cache';
            headers['Content-Length'] = await Utils.FormDataLength(form);

            const urlinfo = _url.parse(endpoint);
            const postReq = http.request({
                host: urlinfo.host,
                port: (urlinfo.port || (urlinfo.protocol === 'https:' ? 443 : 80)),
                path: urlinfo.pathname,
                method: 'POST',
                headers: headers
            },
                response => {
                    fulfill(response.statusCode);
                },
                err => {
                    reject(err);
                }
            );

            form.pipe(postReq);
        }));
    }
}

/// <summary>
/// Names of app bundles on this project
/// </summary>
router.get('/appbundles', async /*GetLocalBundles*/ (req, res) => {
    // this folder is placed under the public folder, which may expose the bundles
    // but it was defined this way so it be published on most hosts easily
    let bundles = await Utils.findFiles(Utils.LocalBundlesFolder, '.zip');
    bundles = bundles.map((fn) => _path.basename(fn, '.zip'));
    res.json(bundles);
});

/// <summary>
/// Return a list of available engines
/// </summary>
router.get('/forge/designautomation/engines', async /*GetAvailableEngines*/ (req, res) => {
    let that = this;
    let Allengines = [];
    let paginationToken = null;
    try {
        const api = await Utils.dav3API(req.oauth_token);
        while (true) {
            let engines = await api.getEngines({'page':paginationToken});
            Allengines = Allengines.concat(engines.data)
            if (engines.paginationToken == null) break;
            paginationToken = engines.paginationToken;
        }
        res.json(Allengines.sort()); // return list of engines
    } catch (ex) {
        console.error(ex);
        res.json([]);
    }

});

/// <summary>
/// Define a new appbundle
/// </summary>
router.post('/forge/designautomation/appbundles', async /*CreateAppBundle*/ (req, res) => {
    const appBundleSpecs = req.body;

    // basic input validation
    const zipFileName = appBundleSpecs.zipFileName;
    const engineName = appBundleSpecs.engine;

    // standard name for this sample
    const appBundleName = zipFileName + 'AppBundle';

    // check if ZIP with bundle is here
    const packageZipPath = _path.join(Utils.LocalBundlesFolder, zipFileName + '.zip');

    // get defined app bundles
    const api = await Utils.dav3API(req.oauth_token);
    let appBundles = null;
    try {
        appBundles = await api.getAppBundles();
    } catch (ex) {
        console.error(ex);
        return (res.status(500).json({
            diagnostic: 'Failed to get the Bundle list'
        }));
    }
    // check if app bundle is already define
    let newAppVersion = null;
    const qualifiedAppBundleId = `${Utils.NickName}.${appBundleName}+${Utils.Alias}`;
    if (!appBundles.data.includes(qualifiedAppBundleId)) {
        // create an appbundle (version 1)
        // const appBundleSpec = {
        //         package: appBundleName,
        //         engine: engineName,
        //         id: appBundleName,
        //         description: `Description for ${appBundleName}`
        //     };
        const appBundleSpec = dav3.AppBundle.constructFromObject({
            package: appBundleName,
            engine: engineName,
            id: appBundleName,
            description: `Description for ${appBundleName}`
        });
        try {
            newAppVersion = await api.createAppBundle(appBundleSpec);
        } catch (ex) {
            console.error(ex);
            return (res.status(500).json({
                diagnostic: 'Cannot create new app'
            }));
        }

        // create alias pointing to v1
        const aliasSpec = //dav3.Alias.constructFromObject({
        {
            id: Utils.Alias,
            version: 1
        };
        try {
            const newAlias = await api.createAppBundleAlias(appBundleName, aliasSpec);
        } catch (ex) {
            console.error(ex);
            return (res.status(500).json({
                diagnostic: 'Failed to create an alias'
            }));
        }
    } else {
        // create new version
        const appBundleSpec = //dav3.AppBundle.constructFromObject({
        {
            engine: engineName,
            description: appBundleName
        };
        try {
            newAppVersion = await api.createAppBundleVersion(appBundleName, appBundleSpec);
        } catch (ex) {
            console.error(ex);
            return (res.status(500).json({
                diagnostic: 'Cannot create new version'
            }));
        }

        // update alias pointing to v+1
        const aliasSpec = //dav3.AliasPatch.constructFromObject({
        {
            version: newAppVersion.version
        };
        try {
            const newAlias = await api.modifyAppBundleAlias(appBundleName, Utils.Alias, aliasSpec);
        } catch (ex) {
            console.error(ex);
            return (res.status(500).json({
                diagnostic: 'Failed to create an alias'
            }));
        }
    }

    // upload the zip with .bundle
    try {
        // curl https://bucketname.s3.amazonaws.com/
        // -F key = apps/myApp/myfile.zip
        // -F content-type = application/octet-stream
        // -F policy = eyJleHBpcmF0aW9uIjoiMjAxOC0wNi0yMVQxMzo...(trimmed)
        // -F x-amz-signature = 800e52d73579387757e1c1cd88762...(trimmed)
        // -F x-amz-credential = AKIAIOSFODNN7EXAMPLE/20180621/us-west-2/s3/aws4_request/
        // -F x-amz-algorithm = AWS4-HMAC-SHA256
        // -F x-amz-date = 20180621T091656Z
        // -F file=@E:myfile.zip
        //
        // The ‘file’ field must be at the end, all fields after ‘file’ will be ignored.
        await Utils.uploadFormDataWithFile(
            packageZipPath,
            newAppVersion.uploadParameters.endpointURL,
            newAppVersion.uploadParameters.formData
        );
    } catch (ex) {
        console.error(ex);
        return (res.status(500).json({
            diagnostic: 'Failed to upload bundle on s3'
        }));
    }

    res.status(200).json({
        appBundle: qualifiedAppBundleId,
        version: newAppVersion.version
    });
});

/// <summary>
/// CreateActivity a new Activity
/// </summary>
router.post('/forge/designautomation/activities', async /*CreateActivity*/ (req, res) => {
    const activitySpecs = req.body;

    // basic input validation
    const zipFileName = activitySpecs.zipFileName;
    const engineName = activitySpecs.engine;

    // standard name for this sample
    const appBundleName = zipFileName + 'AppBundle';
    const activityName = zipFileName + 'Activity';

    // get defined activities
    const api = await Utils.dav3API(req.oauth_token);
    let activities = null;
    try {
        activities = await api.getActivities();
    } catch (ex) {
        console.error(ex);
        return (res.status(500).json({
            diagnostic: 'Failed to get activity list'
        }));
    }
    const qualifiedActivityId = `${Utils.NickName}.${activityName}+${Utils.Alias}`;
    if (!activities.data.includes(qualifiedActivityId)) {
        // define the activity
        // ToDo: parametrize for different engines...
        const engineAttributes = Utils.EngineAttributes(engineName);
        const commandLine = engineAttributes.commandLine.replace('{0}', appBundleName);
        const activitySpec = {
            id: activityName,
            appbundles: [`${Utils.NickName}.${appBundleName}+${Utils.Alias}`],
            commandLine: [commandLine],
            engine: engineName,
            parameters: {
                inputFile: {
                    description: 'input file',
                    localName: '$(inputFile)',
                    ondemand: false,
                    required: true,
                    verb: dav3.Verb.get,
                    zip: false
                },
                inputJson: {
                    description: 'input json',
                    localName: 'params.json',
                    ondemand: false,
                    required: false,
                    verb: dav3.Verb.get,
                    zip: false
                },
                outputFile: {
                    description: 'output file',
                    localName: 'outputFile.' + engineAttributes.extension,
                    ondemand: false,
                    required: true,
                    verb: dav3.Verb.put,
                    zip: false
                }
            },
            settings: {
                script: {
                    value: engineAttributes.script
                }
            }
        };
        try {
            const newActivity = await api.createActivity(activitySpec);
        } catch (ex) {
            console.error(ex);
            return (res.status(500).json({
                diagnostic: 'Failed to create new activity'
            }));
        }
        // specify the alias for this Activity
        const aliasSpec = {
            id: Utils.Alias,
            version: 1
        };
        try {
            const newAlias = await api.createActivityAlias(activityName, aliasSpec);
        } catch (ex) {
            console.error(ex);
            return (res.status(500).json({
                diagnostic: 'Failed to create new alias for activity'
            }));
        }
        res.status(200).json({
            activity: qualifiedActivityId
        });
        return;
    }

    // as this activity points to a AppBundle "dev" alias (which points to the last version of the bundle),
    // there is no need to update it (for this sample), but this may be extended for different contexts
    res.status(200).json({
        activity: 'Activity already defined'
    });
});

/// <summary>
/// Get all Activities defined for this account
/// </summary>
router.get('/forge/designautomation/activities', async /*GetDefinedActivities*/ (req, res) => {
    const api = await Utils.dav3API(req.oauth_token);
    // filter list of 
    let activities = null;
    try {
        activities = await api.getActivities();
    } catch (ex) {
        console.error(ex);
        return (res.status(500).json({
            diagnostic: 'Failed to get activity list'
        }));
    }
    let definedActivities = [];
    for (let i = 0; i < activities.data.length; i++) {
        let activity = activities.data[i];
        if (activity.startsWith(Utils.NickName) && activity.indexOf('$LATEST') === -1)
            definedActivities.push(activity.replace(Utils.NickName + '.', ''));
    }

    res.status(200).json(definedActivities);
});

/// <summary>
/// Start a new workitem
/// </summary>
router.post('/forge/designautomation/workitems', multer({
    dest: 'uploads/'
}).single('inputFile'), async /*StartWorkitem*/ (req, res) => {
    const input = req.body;

    // basic input validation
    const workItemData = JSON.parse(input.data);
    const widthParam = parseFloat(workItemData.width);
    const heigthParam = parseFloat(workItemData.height);
    const activityName = `${Utils.NickName}.${workItemData.activityName}`;
    const browerConnectionId = workItemData.browerConnectionId;

    // save the file on the server
    const ContentRootPath = _path.resolve(_path.join(__dirname, '../..'));
    const fileSavePath = _path.join(ContentRootPath, _path.basename(req.file.originalname));
    //const stream = _fs.createReasStream(fileSavePath, FileMode.Create)) await input.inputFile.CopyToAsync(stream);

    // upload file to OSS Bucket
    // 1. ensure bucket existis
    const bucketKey = Utils.NickName.toLowerCase() + '-designautomation';
    try {
        let payload = new ForgeAPI.PostBucketsPayload();
        payload.bucketKey = bucketKey;
        payload.policyKey = 'transient'; // expires in 24h
        await new ForgeAPI.BucketsApi().createBucket(payload, {}, req.oauth_client, req.oauth_token);
    } catch (ex) {
        // in case bucket already exists
    }
    // 2. upload inputFile
    const inputFileNameOSS = `${new Date().toISOString().replace(/[-T:\.Z]/gm, '').substring(0, 14)}_input_${_path.basename(req.file.originalname)}`; // avoid overriding
    try {
        let contentStream = _fs.createReadStream(req.file.path);
        await new ForgeAPI.ObjectsApi().uploadObject(bucketKey, inputFileNameOSS, req.file.size, contentStream, {}, req.oauth_client, req.oauth_token);
    } catch (ex) {
        console.error(ex);
        return (res.status(500).json({
            diagnostic: 'Failed to upload file for workitem'
        }));
    }

    // prepare workitem arguments
    // 1. input file
    const inputFileArgument = {
        url: `https://developer.api.autodesk.com/oss/v2/buckets/${bucketKey}/objects/${inputFileNameOSS}`,
        headers: {
            Authorization: `Bearer ${req.oauth_token.access_token}`
        }
    };
    // 2. input json
    const inputJson = {
        width: widthParam,
        height: heigthParam
    };
    const inputJsonArgument = {
        url: "data:application/json, " + JSON.stringify(inputJson).replace(/"/g, "'")
    };
    // 3. output file
    // const outputFileNameOSS = `${new Date().toISOString().replace (/[-T:\.Z]/gm, '').substring(0, 14)}_output_${_path.basename(req.file.originalname)}`; // avoid overriding
    // const outputFileArgument = {
    //     url: `https://developer.api.autodesk.com/oss/v2/buckets/${bucketKey}/objects/${outputFileNameOSS}`,
    //     verb: dav3.Verb.put,
    //     headers: {
    //         Authorization: `Bearer ${req.oauth_token.access_token}`
    //     }
    // };

    // Better to use a presigned url to avoid the token to expire
    const outputFileNameOSS = `${new Date().toISOString().replace(/[-T:\.Z]/gm, '').substring(0, 14)}_output_${_path.basename(req.file.originalname)}`; // avoid overriding
    let signedUrl = null;
    try {
        // write signed resource requires the object to already exist :(
        await new ForgeAPI.ObjectsApi().copyTo(bucketKey, inputFileNameOSS, outputFileNameOSS, req.oauth_client, req.oauth_token);
        signedUrl = await new ForgeAPI.ObjectsApi().createSignedResource(
            bucketKey,
            outputFileNameOSS, {
            minutesExpiration: 60,
            singleUse: true
        }, {
            access: 'write'
        },
            req.oauth_client, req.oauth_token
        );
        signedUrl = signedUrl.body.signedUrl;
    } catch (ex) {
        console.error(ex);
        return (res.status(500).json({
            diagnostic: 'Failed to create a signed URL for output file'
        }));
    }
    const outputFileArgument = {
        //url: `https://developer.api.autodesk.com/oss/v2/buckets/${bucketKey}/objects/${outputFileNameOSS}`,
        url: signedUrl,
        headers: {
            Authorization: '',
            'Content-type': 'application/octet-stream'
        },
        verb: dav3.Verb.put,
    };

    // prepare & submit workitem
    // the callback contains the connectionId (used to identify the client) and the outputFileName of this workitem
    const callbackUrl = `${config.credentials.webhook_url}/api/forge/callback/designautomation?id=${browerConnectionId}&outputFileName=${outputFileNameOSS}&inputFileName=${inputFileNameOSS}`;
    const workItemSpec = {
        activityId: activityName,
        arguments: {
            inputFile: inputFileArgument,
            inputJson: inputJsonArgument,
            outputFile: outputFileArgument,
            onComplete: {
                verb: dav3.Verb.post,
                url: callbackUrl
            }
        }
    };
    let workItemStatus = null;
    try {
        const api = await Utils.dav3API(req.oauth_token);
        workItemStatus = await api.createWorkItem(workItemSpec);
    } catch (ex) {
        console.error(ex);
        return (res.status(500).json({
            diagnostic: 'Failed to create a workitem'
        }));
    }
    res.status(200).json({
        workItemId: workItemStatus.id
    });
});

/// <summary>
/// Callback from Design Automation Workitem (onProgress or onComplete)
/// </summary>
router.post('/forge/callback/designautomation', async /*OnCallback*/ (req, res) => {
    // your webhook should return immediately! we could use Hangfire to schedule a job instead
    // ALWAYS return ok (200)
    res.status(200).end();

    try {
        const socketIO = require('../server').io;

        // your webhook should return immediately! we can use Hangfire to schedule a job
        const bodyJson = req.body;
        socketIO.to(req.query.id).emit('onComplete', bodyJson);

        http.get(
            bodyJson.reportUrl,
            response => {
                //socketIO.to(req.query.id).emit('onComplete', response);
                response.setEncoding('utf8');
                let rawData = '';
                response.on('data', (chunk) => {
                    rawData += chunk;
                });
                response.on('end', () => {
                    socketIO.to(req.query.id).emit('onComplete', rawData);
                });
            }
        );
        //socketIO.to(req.query.id).emit('downloadReport', bodyJson.reportUrl);

        const objectsApi = new ForgeAPI.ObjectsApi();
        const bucketKey = Utils.NickName.toLowerCase() + '-designautomation';
        if (bodyJson.status === 'success') {
            try {
                // generate a signed URL to download the result file and send to the client
                const signedUrl = await objectsApi.createSignedResource(
                    bucketKey,
                    req.query.outputFileName, {
                    minutesExpiration: 10,
                    singleUse: false
                }, {
                    access: 'read'
                },
                    req.oauth_client, req.oauth_token
                );
                socketIO.to(req.query.id).emit('downloadResult', signedUrl.body.signedUrl);
            } catch (ex) {
                console.error(ex);
                socketIO.to(req.query.id).emit('onComplete', 'Failed to create presigned URL for outputFile.\nYour outputFile is available in your OSS bucket.');
            }
        }

        // delete the input file (we do not need it anymore)
        try {
            /*await*/
            objectsApi.deleteObject(bucketKey, req.query.inputFileName, req.oauth_client, req.oauth_token);
        } catch (ex) {
            console.error(ex);
        }

    } catch (ex) {
        console.error(ex);
    }
});

/// <summary>
/// Clear the accounts (for debugging purpouses)
/// </summary>
router.delete('/forge/designautomation/account', async /*ClearAccount*/ (req, res) => {
    let api = await Utils.dav3API(req.oauth_token);
    // clear account
    await api.deleteForgeApp('me');
    res.status(200).end();
});

module.exports = router;