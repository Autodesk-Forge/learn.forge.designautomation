# learn.forge.designautomation - 3ds Max

## Tutorial to prepare a 3ds Max .NET Application bundle that can be consumed in Forge Design Automation

### Step 1: Build a .NET 3ds Max assembly. C++, Python, and MAXScript are also supported.

Note that Design Automation cannot have any user interaction requirements. For .NET programs, the easiest way to automate them is to make a static class and then execute the functions from MAXScript. In this example, you will find the automation command in the .\learn.forge.designautomation\UpdateMAXParam\UpdateMAXParam.bundle\da_script.ms

The example is provided here: .\learn.forge.designautomation\UpdateMAXParam


### Step 2: Test your application locally. You can use the 3ds Max Batch tool for this. 

The 3ds Max desktop software already includes a tool called 3ds Max Batch. This tool allows local automation of tasks and is a good way to test your automation before using the Design Automation system. See here for the 3ds Max 2019 Batch details: http://help.autodesk.com/view/3DSMAX/2019/ENU/?guid=GUID-0968FF0A-5ADD-454D-B8F6-1983E76A4AF9

Anything that runs in the 3ds Max Batch tool is possible to run in the 3ds Max Design Automation engine. You can also include plug-ins that can be automated. Typically, you would provide a MAXScript that would execute the automation, but the code itself could live in a C++ or a .NET plug-in. Of course, the MAXScript could also contain the automation code. The Design Automation API also supports the ability to pass parameters into the script code, that can also be passed into the automation code. This will allow very flexible work-flows with the ability to provide configurations and data to the automation job. For example, you could have a configurator website that takes information from your customer, then using 3ds Max Design Automation you could auto-generate a max model to give back to your customer. Using other Forge APIs, you could also preview the model using the Forge Model Derivative service and the Forge Viewer.


### Step 3: Building the AppPackage .bundle

The "Autoloader" plugin mechanism simplifies deployment of your plugin applications to Forge Design Automation. This allows you to deploy your plugins using the simple package format (a folder structure with a .bundle extension) along with an XML file placed in the root of the folder structure. The XML file contains metadata that describes the components of your plugin inside the folder structure, and how they should be loaded.

Full reference of [PackageContents.xml](https://knowledge.autodesk.com/search-result/caas/CloudHelp/cloudhelp/2015/ENU/AutoCAD-Customization/files/GUID-BC76355D-682B-46ED-B9B7-66C95EEF2BD0-htm.html) and [Bundle](https://knowledge.autodesk.com/search-result/caas/CloudHelp/cloudhelp/2015/ENU/AutoCAD-Customization/files/GUID-40F5E92C-37D8-4D54-9497-CD9F0659F9BB-htm.html) structure.

1. Create a UpdateMAXParameters.bundle folder within same project folder

2. Create a Contents folder within UpdateMAXParameters.bundle, this contains module of our UpdateParameters application.

3. Create a PackageContents.xml, this contains metadata of the application module to loaded by Forge Design Automation. See the example file here: .\learn.forge.designautomation\UpdateMAXParam\UpdateMAXParam.bundle\PackageContents.xml.

4. Wrap the UpdateMAXParameters.bundle into a zip file. This can be done in the "post build event" setting using the zip tool of your choice (for example, [7z zip tool](https://www.7-zip.org/)). See the example project .\learn.forge.designautomation\UpdateMAXParam\UpdateMAXParam.csproj file in the PostBuildEvent section.

