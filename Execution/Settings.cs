using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimkravRvt.UserInterface;
using System.Windows.Interop;
using System.Collections.Generic;
using System;
using System.Net;
using BimkravRvt.Utils;

namespace BimkravRvt.Execution
{
    //Set the attributes
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    class SettingsBimkrav : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            App.APIEndPoint = Registry.Read<string>(App.REGISTRY_PATH, "APIEndPoint", App.APIEndPoint);
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            if (App.API.Uri != App.APIEndPoint)
                App.ViewModel = null;
            if (App.ViewModel == null)
                App.ViewModel = new SettingsVM(doc);

            if (!App.ViewModel.IsConnectionOK)
            {
                App.ViewModel = new SettingsVM(doc);
            }
            if (!App.ViewModel.IsConnectionOK)
            {
               if(App.ViewModel.Connection != Connection.MariaDB)
                TaskDialog.Show("Bimkrav", $"Error in importing parameters. Not able to connect to the api. Cant connect to {App.APIEndPoint} ");
               else
                TaskDialog.Show("Bimkrav", "Error in importing parameters. Not able to connect to database. There might be a problem with your firewall settings.");
                return Result.Cancelled;
            }

            var win = new SettingsWin()
            { DataContext = App.ViewModel };
            _ = new WindowInteropHelper(win)
            { Owner = uidoc.Application.MainWindowHandle };
            var result = win.ShowDialog().Value;
            if (!result)
                return Result.Cancelled;

            return Result.Succeeded;
        }


        public static void CheckSharedSettings(Document doc)
        {
            if (App.ViewModel == null)
                App.ViewModel = new SettingsVM(doc);
            App.ViewModel.SharedSettingsOK = false;

            var parameter = doc.ProjectInformation.get_Parameter(Utils.Utils.GetSharedParameterGUID());
            if (parameter == null)
                return;
            var path = parameter.AsString();
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return;

            parameter = doc.ProjectInformation.get_Parameter(Utils.Utils.GetPhaseParameterGUID());
            if (parameter == null)
                return;
            var value = parameter.AsInteger();
            if (value < 100)
                return;

            parameter = doc.ProjectInformation.get_Parameter(Utils.Utils.GetDisciplineParameterGUID());
            if (parameter == null)
                return;
            var disciplineTag = parameter.AsString();
            if (string.IsNullOrEmpty(disciplineTag))
                return;

            parameter = doc.ProjectInformation.get_Parameter(Utils.Utils.GetProjectParameterGUID());
            if (parameter == null)
                return;
            var projectTag = parameter.AsString();
            if (string.IsNullOrEmpty(projectTag))
                return;

            App.ViewModel.SharedSettingsOK = true;
            return;
        }
        public static void CheckIfcSettings(Document doc)
        {
            if (App.ViewModel == null)
                App.ViewModel = new SettingsVM(doc);
            App.ViewModel.IfcSettingsOK = false;

            var parameter = doc.ProjectInformation.get_Parameter(Utils.Utils.GetIFCParameterGUID());
            if (parameter == null)
                return;
            var path = parameter.AsString();
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return;

            parameter = doc.ProjectInformation.get_Parameter(Utils.Utils.GetPhaseParameterGUID());
            if (parameter == null)
                return;
            var value = parameter.AsInteger();
            if (value < 100)
                return;

            parameter = doc.ProjectInformation.get_Parameter(Utils.Utils.GetDisciplineParameterGUID());
            if (parameter == null)
                return;
            var disciplineTag = parameter.AsString();
            if (string.IsNullOrEmpty(disciplineTag))
                return;

            parameter = doc.ProjectInformation.get_Parameter(Utils.Utils.GetProjectParameterGUID());
            if (parameter == null)
                return;
            var projectTag = parameter.AsString();
            if (string.IsNullOrEmpty(projectTag))
                return;

            App.ViewModel.IfcSettingsOK = true;
            return;
        }

    }
}
