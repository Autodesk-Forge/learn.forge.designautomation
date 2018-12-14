using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using DesignAutomationFramework;

namespace RevitDesignAutomation
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class DesignAutomationClass : IExternalDBApplication
    {
        //Path of the project(i.e)project where your Window family files are present
        string projectPath = "\\WindowInWall.rvt";

        //The parameters value from the user
        double windowHeight = 5;
        double windowWidth =10;

        public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
        {
            return ExternalDBApplicationResult.Succeeded;
        }

        public ExternalDBApplicationResult OnStartup(ControlledApplication application)
        {
            application.ApplicationInitialized += HandleApplicationInitializedEvent;
            return ExternalDBApplicationResult.Succeeded;
        }

        private void HandleApplicationInitializedEvent(object sender, ApplicationInitializedEventArgs e)
        {
            Autodesk.Revit.ApplicationServices.Application app = sender as Autodesk.Revit.ApplicationServices.Application;
            EditWindowParametersMethod(app, projectPath);
        }

        private void EditWindowParametersMethod(Application app, string projectPath)
        {
            // Get the data of the project file(i.e)the project in which families are present
            DesignAutomationData data = new DesignAutomationData(app, projectPath);
            //get the revit project document
            Document doc = data.RevitDoc;

            //Modifying the window parameters
            //Open transaction
            using (Transaction windowTransaction = new Transaction(doc))
            {
                windowTransaction.Start("Update window parameters");

                //Filter for windows
                FilteredElementCollector WindowCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType();
                IList<ElementId> windowIds = WindowCollector.ToElementIds() as IList<ElementId>;

                foreach (ElementId windowId in windowIds)
                {
                    Element Window = doc.GetElement(windowId);
                    FamilyInstance FamInst = Window as FamilyInstance;
                    FamilySymbol FamSym = FamInst.Symbol;
                    SetElementParameter(FamSym, BuiltInParameter.WINDOW_HEIGHT, windowHeight);
                    SetElementParameter(FamSym, BuiltInParameter.WINDOW_WIDTH, windowWidth);
                }

                //To save all the changes commit the transaction 
                windowTransaction.Commit();
            }

            //Save the updated file by overwriting the existing file
            ModelPath ProjectModelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(projectPath);
            SaveAsOptions SAO = new SaveAsOptions();
            SAO.OverwriteExistingFile = true;

            //Save the project file with updated window's parameters
            doc.SaveAs(ProjectModelPath, SAO);
        }

        private void SetElementParameter(FamilySymbol FamSym, BuiltInParameter paraMeter, double parameterValue)
        {
            FamSym.get_Parameter(paraMeter).Set(parameterValue);
        }
    }
}
