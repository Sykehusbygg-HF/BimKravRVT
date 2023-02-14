using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace BimkravRvt.Utils
{
    class Ribbon
    {
        /// <summary>
        /// Loads an image asset from Resource folder. Asset has to be an EmbeddedResource type.
        /// </summary>
        /// <param name="assembly">Assembly to load an asset from.</param>
        /// <param name="nameSpace">Namespace that asset is located in. Usually namespace of main class.</param>
        /// <param name="imageName">String name of the asset including file extension.</param>
        /// <returns>BitmapImage asset that can be used as Image for Revit button.</returns>
        public static BitmapImage LoadImage(Assembly assembly, string nameSpace, string imageName)
        {
            var img = new BitmapImage();
            try
            {
                var prefix = nameSpace + ".Resources.";
                var stream = assembly.GetManifestResourceStream(prefix + imageName);

                img.BeginInit();
                img.StreamSource = stream;
                img.EndInit();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }
            return img;
        }

        public static bool CreateTab(string tabName)
        {
            try
            {
                App.uiapp.CreateRibbonTab(tabName);
                return true;
            }
            catch { return false; }
        }

        public static RibbonPanel CreatePanel(string tabName, string panelName)
        {
            List<RibbonPanel> panels = App.uiapp.GetRibbonPanels(tabName);
            RibbonPanel panel = null;
            foreach (RibbonPanel p in panels)
            {
                if (p.Name == panelName)
                {
                    panel = p;
                    break;
                }
            }
            if (panel == null)
            {
                panel = App.uiapp.CreateRibbonPanel(tabName, panelName);
            }
            return panel;
        }
    }
}
