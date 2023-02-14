using Autodesk.Revit.UI;
using BimkravRvt.Execution;
using BimkravRvt.UserInterface;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System;
using BIMLookup.NetApi;
using System.Net;
using BimkravRvt.Utils;

namespace BimkravRvt
{
    public class App : IExternalApplication
    {
        internal static Assembly assembly = Assembly.GetExecutingAssembly();
        internal static string assemblyPath = assembly.Location;
        internal static string assemblyVer = ((AssemblyFileVersionAttribute)assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).Single()).Version;
        internal static UIControlledApplication uiapp;
        internal static List<PushButton> ConnectionDependentButtons = new List<PushButton>();
        internal static SettingsVM ViewModel = null;
        //TODO: Remove from here
        internal static string APIUserName { get { return $"APIServiceUser"; } }
        internal static string APIPassword { get { return $""; } }
        public const string REGISTRY_PATH = "Software\\Sykehusbygg\\bimkrav";
#if DEBUG
        internal static string APIEndPoint {get;set; } = $"https://bimlookup.azurewebsites.net/api/"; //{ get { return $"https://s-app-nea-bimkrav-prod.azurewebsites.net/api/"; } }// $"https://localhost:44318/api/"; } }
#else
        internal static string APIEndPoint {get;set; } = $"https://bimkrav.sykehusbygg.no/api/"; 
#endif
        internal static BIMLookupAPI API = null;


        public Result OnStartup(UIControlledApplication application)
        {
            APIEndPoint = Registry.Read<string>(REGISTRY_PATH, "APIEndPoint", APIEndPoint);
#if DEBUG
            Registry.SaveString(REGISTRY_PATH, "APIEndPoint", APIEndPoint);
#endif
            uiapp = application;
            var app = uiapp.ControlledApplication;
            var tabName = "Sykehusbygg";
            var panelName = "Bimkrav";


            Utils.Ribbon.CreateTab(tabName);
            RibbonPanel panel = Utils.Ribbon.CreatePanel(tabName, panelName);

            var importButton = new PushButtonData("Bimkrav_Import", "Import", assemblyPath, typeof(ImportParameters).FullName)
            {
                LargeImage = Utils.Ribbon.LoadImage(assembly, typeof(App).Namespace, "import32.png"),
                Image = Utils.Ribbon.LoadImage(assembly, typeof(App).Namespace, "import16.png"),
                ToolTip = "Creates shared parameters in text file and imports parameters to project.",
                AvailabilityClassName = typeof(ImportAvailable).FullName
            };
            _ = panel.AddItem(importButton) as PushButton;

            var ifcSetupButton = new PushButtonData("Bimkrav_IFCSetup", "IFC\nSetup", assemblyPath, typeof(CreateIFCSetup).FullName)
            {
                LargeImage = Utils.Ribbon.LoadImage(assembly, typeof(App).Namespace, "ifcsetup32.png"),
                Image = Utils.Ribbon.LoadImage(assembly, typeof(App).Namespace, "ifcsetup16.png"),
                ToolTip = "Creates ifc export setup file.",
                AvailabilityClassName = typeof(IfcAvailable).FullName
            };
            _ = panel.AddItem(ifcSetupButton) as PushButton;

            var settingsButton = new PushButtonData("Bimkrav_Settings", "Settings", assemblyPath, typeof(SettingsBimkrav).FullName)
            {
                LargeImage = Utils.Ribbon.LoadImage(assembly, typeof(App).Namespace, "settings32.png"),
                Image = Utils.Ribbon.LoadImage(assembly, typeof(App).Namespace, "settings16.png"),
                ToolTip = "Settings for Bimkrav-addin."
            };
            _ = panel.AddItem(settingsButton) as PushButton;

            app.DocumentOpened += App_DocumentOpened;
            uiapp.ViewActivated += Uiapp_ViewActivated;

            return Result.Succeeded;
        }


        public Result OnShutdown(UIControlledApplication application)
        {
            ViewModel = null;
            var app = uiapp.ControlledApplication;
            app.DocumentOpened -= App_DocumentOpened;
            uiapp.ViewActivated -= Uiapp_ViewActivated;
            return Result.Succeeded;
        }

        private void Uiapp_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
        {
            if (e.PreviousActiveView == null || !e.CurrentActiveView.Document.Equals(e.PreviousActiveView.Document))
            {
                ViewModel = null;
                SettingsBimkrav.CheckIfcSettings(e.CurrentActiveView.Document);
                SettingsBimkrav.CheckSharedSettings(e.CurrentActiveView.Document);
            }
        }

        private void App_DocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
        {
            ViewModel = null;
            SettingsBimkrav.CheckIfcSettings(e.Document);
            SettingsBimkrav.CheckSharedSettings(e.Document);
        }


    }
}
