

# learn.forge.designautomation - AutoCAD

## This is a step by step tutorial to prepare AutoCAD Application bundle which can be consumed in Forge Design Automation

### Step1: Launch Visual Studio 2017

Launch Visual Studio 2017 Community or Professional, create a new C# class library.

![](https://github.com/MadhukarMoogala/learn.forge.designautomation/blob/master\autocad\images\LaunchVS2017.JPG)

### Step2: Install AutoCAD .NET Packages for AutoCAD 2019 from Nuget Package Manager console.

![](https://github.com/MadhukarMoogala/learn.forge.designautomation/blob/master\autocad\images\nugetForAutoCAD.JPG)

### Step3: Nuget package will install many modules, most of theses modules are not required for Forge Design Automation,  keep the relevant ones and remove the rest.

![](https://github.com/MadhukarMoogala/learn.forge.designautomation/blob/master\autocad\images\ModulesNotRequiredForForge.JPG)

### Step4 :  Write C# .NET application to update parameters Width and Height of a dynamic block reference, refer code.

### Step 5:  Debugging locally your .NET module before making and uploading Application Package Bundle to Forge.

1. In the solution explorer, right click on Project and go to Properties.

2. Go to Debug page, in `start external program`, pass the path AcCoreConsole.exe `C:\Program Files\Autodesk\AutoCAD 2019\accoreconsole.exe`

3. Go to Command Line arguments, 

   ```
   /i "D:\Forge\learn.forge.designautomation/learn.forge.designautomation/blob/master\autocad\testDrawing\WindowTest.dwg"
   /s "D:\Forge\learn.forge.designautomation/learn.forge.designautomation/blob/master\autocad\testDrawing\window.scr"
   ```

   /i: input drawing

   /s: to load script file.

   script file is helpful way to load the built module and run the command and other parameters, for example.

   ```
   SECURELOAD
   0
   NETLOAD
   D:\Forge\learn.forge.designautomation\autocad\UpdateWindowParameters\bin\Debug\UpdateWindowParameters.dll
   UpdateWindowParam
   WindowBlock
   40.00
   80.00
   SAVEAS
   2018
   D:\Forge\learn.forge.designautomation\autocad\testDrawing\windowNew.dwg
   ```

   ![](https://github.com/MadhukarMoogala/learn.forge.designautomation/blob/master\autocad\images\LocalDebug.JPG)

### Step 6: Building AppPackage .bundle

   The “Autoloader” plugin mechanism simplifies deployment of your plugin applications to Forge, This is done by
   allowing you to deploy your plugins as a simple package format (a folder structure with a .bundle
   extension) along with an XML file placed in the root of the folder structure. The XML file contains
   metadata which describes the components of your plugin inside the folder structure, and how they should
   be loaded.

   ![](https://github.com/MadhukarMoogala/learn.forge.designautomation/blob/master\autocad\images\BundleStructure.JPG)



   Full reference of [PackageContents.xml](https://knowledge.autodesk.com/search-result/caas/CloudHelp/cloudhelp/2015/ENU/AutoCAD-Customization/files/GUID-BC76355D-682B-46ED-B9B7-66C95EEF2BD0-htm.html) and [Bundle](https://knowledge.autodesk.com/search-result/caas/CloudHelp/cloudhelp/2015/ENU/AutoCAD-Customization/files/GUID-40F5E92C-37D8-4D54-9497-CD9F0659F9BB-htm.html) structure.

   1. Create a UpdateParameters.bundle folder within same project folder

   2. Create a Contents folder within UpdateParameters.bundle, this contains module of our UpdateParameters application.

   3. Create a PackageContents.xml, this contains metadata of the application module to loaded by Forge Design Automation

      ```xml
      <?xml version="1.0" encoding="utf-8" ?>
      <ApplicationPackage
          SchemaVersion="1.0"
          Version="1.0"
          ProductCode="{F11EA57A-1E7E-4B6D-8E81-986B071E3E07}"
          Name="UpdateWindowParameters"
          Description="A sample package to update parameters of a Dyanmic blockreference"
          Author="Autodesk Forge">
        <CompanyDetails
            Name="Autodesk, Inc"
            Phone="12345678910"
            Url="www.autodesk.com"
            Email="forge.help@autodesk.com"/>
        <Components>
          <RuntimeRequirements
              OS="Win64"
              Platform="AutoCAD"
      		SeriesMin="R23.0" SeriesMax="R23.0"/>
          <ComponentEntry
              AppName="UpdateWindowParameters"
              ModuleName="./Contents/UpdateWindowParameters.dll"
              AppDescription="AutoCAD.IO .net App to update parameters of Dynamic blockreference in AutoCAD Drawing"
              LoadOnCommandInvocation="True"
              LoadOnAutoCADStartup="False">
            <Commands GroupName="FPDCommands">
              <Command Global="UpdateWindowParam" Local="UpdateWindowParam" />
            </Commands>
          </ComponentEntry>
        </Components>
      </ApplicationPackage>
      ```

4. Wrap the UpdateParameters.bundle to bundle.zip, this can be done is `post build event`, you can install free [7z zip tool](https://www.7-zip.org/)

   ```bash
   xcopy /Y $(TargetPath) $(ProjectDir)UpdateWindowParemeters.bundle\Contents\
   if exist $(ProjectDir)bundle.zip (del bundle.zip /q)
   7z a -tzip $(ProjectDir)bundle.zip  $(ProjectDir)UpdateWindowParemeters.bundle\ -xr0!*.pdb
   ```


