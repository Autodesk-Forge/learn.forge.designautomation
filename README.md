# learn.forge.designautomation

## Description

This sample is (will be) part of the [Learn Forge](http://learnforge.autodesk.io) tutorials.

It includes 5 modules:

- .NET Framework plugins for **[AutoCAD](UpdateDWGParam/)**, **[Inventor](UpdateIPTParam/)**, **[Revit](UpdateRVTParam/)** and **[3dsMax](UpdateMAXParam/)**. See each readme for plugin details.
- .NET Core web interface to invoke Design Automation v3 and show results. See [readme](forgesample/) for more information.

The `designautomation.sln` include all 4 bundles and the webapp, you may unload those project that are not of interest. The `BUILD` action copy all files to the bundle folder, generate a .zip and copy to the webapp folder. It requires [7-zip](https://www.7-zip.org/) tool.
