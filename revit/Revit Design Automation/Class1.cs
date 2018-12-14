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

namespace Revit_Design_Automation
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Class1 : IExternalDBApplication
    {
        //Path of the project(i.e)project where your Window family files are present
        string Project_Path = "//WindowInWall.rvt";

        //The parameters value from the user
        double window_Height = 10;
        double window_width = 5;

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
            Edit_Window_parameters_method(app, Project_Path);
        }

        private void Edit_Window_parameters_method(Application app, string project_Path)
        {
            // Get the data of the project file(i.e)the project in which families are present
            DesignAutomationData data = new DesignAutomationData(app, Project_Path);

            //get the revit project document
            Document doc = data.RevitDoc;

            //Modifying the window parameters
            //Open transaction
            Transaction window_transaction = new Transaction(doc);
            window_transaction.Start("Update window parameters");

            //Filter for windows
            FilteredElementCollector Window_collector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType();
            IList<ElementId> window_ids = Window_collector.ToElementIds() as IList<ElementId>;

            foreach (ElementId window_id in window_ids)
            {
                Element Window = doc.GetElement(window_id);
                FamilyInstance fi = Window as FamilyInstance;
                FamilySymbol fs = fi.Symbol;
                SetElementParameter(fs, BuiltInParameter.WINDOW_HEIGHT, "Window Height", window_Height);
                SetElementParameter(fs, BuiltInParameter.WINDOW_WIDTH, "Window Width", window_width);
            }

            //To save all the changes commit the transaction 
            window_transaction.Commit();

            //Save the updated file by overwriting the existing file
            ModelPath Project_model_path = ModelPathUtils.ConvertUserVisiblePathToModelPath(project_Path);
            SaveAsOptions SAO = new SaveAsOptions();
            SAO.OverwriteExistingFile = true;

            //Save the project file with updated window's parameters
            doc.SaveAs(Project_model_path, SAO);
        }

        private void SetElementParameter(FamilySymbol fs, BuiltInParameter parameter, string v, double parameter_value)
        {
            fs.get_Parameter(parameter).Set(parameter_value);
        }
    }
}
