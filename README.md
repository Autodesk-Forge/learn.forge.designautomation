# learn.forge.designautomation - nodejs

[![Node.js](https://img.shields.io/badge/Node.js-4.4.3-blue.svg)](https://nodejs.org/)
[![npm](https://img.shields.io/badge/npm-2.15.1-blue.svg)](https://www.npmjs.com/)
![Platforms](https://img.shields.io/badge/platform-windows%20%7C%20osx%20%7C%20linux-lightgray.svg)
[![License](http://img.shields.io/:license-mit-blue.svg)](http://opensource.org/licenses/MIT)

[![oAuth2](https://img.shields.io/badge/oAuth2-v1-green.svg)](http://developer.autodesk.com/)
[![Data-Management](https://img.shields.io/badge/Data%20Management-v1-green.svg)](http://developer.autodesk.com/)
[![Design-Automation](https://img.shields.io/badge/Design%20Automation-v3-green.svg)](http://developer.autodesk.com/)

![Basic](https://img.shields.io/badge/Level-Basic-blue.svg)

# Description

Webapp interface that uses Design Automation to update the `width` and `height` param of DWG Dynamic Block (using AutoCAD), RVT Window Family instance (using Revit), IPT Part parameters (using Inventor) and 3DS ... (using 3dsMax). Also includes a `Clear Account` (remove all AppBundles and Activities) and `Define new Activity` based on the sample code.

See other projects for bundles.

## Thumbnail

![thumbnail](thumbnail.gif)

# Setup

To use this sample, you will need Autodesk developer credentials. Visit the [Forge Developer Portal](https://developer.autodesk.com), sign up for an account, then [create an app](https://developer.autodesk.com/myapps/create). For this new app, use `http://localhost:3000/api/forge/callback/oauth` as the Callback URL, although it is not used on a 2-legged flow. Finally, take note of the **Client ID** and **Client Secret**.

**ngrok**

When a `Workitem` completes, **Design Automation** can notify our application. As the app is running locally (i.e. `localhost`), it's not reacheable from the internet. `ngrok` tool creates a temporary address that channels notifications to our `localhost` address.

After [download ngrok](https://ngrok.com/), run `ngrok http 3000 -host-header="localhost:3000"`, then copy the `http` address into the `FORGE_WEBHOOK_URL` environment variable (see next). For this sample, do not use the `https` address.

![](../media/webapp/ngrok_setup.png)

## Running locally

Install [NodeJS](https://nodejs.org).

Clone this project or download it. It's recommended to install [GitHub Desktop](https://desktop.github.com/). To clone it via command line, use the following (**Terminal** on MacOSX/Linux, **Git Shell** on Windows):

    git clone -b nodejs https://github.com/Autodesk-Forge/learn.forge.designautomation.git


To run it, install the required packages, set the enviroment variables with your client ID, Secret and ngrok url and finally start it. Via command line, navigate to the folder where this repository was cloned to and use the following commands:

Mac OSX/Linux (Terminal)

    npm install
    export FORGE_CLIENT_ID=<<YOUR CLIENT ID FROM DEVELOPER PORTAL>>
    export FORGE_CLIENT_SECRET=<<YOUR CLIENT SECRET>>
    export FORGE_WEBHOOK_URL=<<YOUR NGROK URL>>
    npm start

Windows (use **Node.js command line** from the Start menu)

    npm install
    set FORGE_CLIENT_ID=<<YOUR CLIENT ID FROM DEVELOPER PORTAL>>
    set FORGE_CLIENT_SECRET=<<YOUR CLIENT SECRET>>
    set FORGE_WEBHOOK_URL=<<YOUR NGROK URL>>
    npm start

Open the browser: [http://localhost:3000](http://localhost:3000).

**How to use this sample**

Open `http://localhost:3000` to start the app, if first time, click on `Configure`(right top) select the bundle and engine and click on `Create/Update` this will create an activity. Enter new `width` & `height` values, select the input file (use from **sample files** folder), select the `Activity` and, finally, `Start workitem`. 

# Further Reading

Documentation:

- [Design Automation v3](https://forge.autodesk.com/en/docs/design-automation/v3/developers_guide/overview/)
- [Data Management](https://forge.autodesk.com/en/docs/data/v2/reference/http/) used to store input and output files.

## Packages used

The [Autodesk Forge](https://www.npmjs.com/package/forge-apis) packages are included by default. Some other non-Autodesk packages are used, including [express](https://www.npmjs.com/package/express) and [multer](https://www.npmjs.com/package/multer) for upload.

# Tips & tricks

For local development/ testing, consider using the [nodemon](https://www.npmjs.com/package/nodemon) package, which auto-restarts your node application after any modification to your code. To install it, use:

    sudo npm install -g nodemon

Then, instead of **npm run dev**, use the following:

    npm run nodemon

Which executes **nodemon server.js --ignore www/**, where the **--ignore** parameter indicates that the app should not restart if files under the **www** folder are modified.

## Troubleshooting

After installing GitHub Desktop for Windows, on the Git Shell, if you see the ***error setting certificate verify locations*** error, then use the following command:

    git config --global http.sslverify "false"

# License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for full details.

## Written by

Cyrille Fauvel  
[@FAUVELCyrille](https://twitter.com/FAUVELCyrille),  
[Forge Partner Development](http://forge.autodesk.com)
