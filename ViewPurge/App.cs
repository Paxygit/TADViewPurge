#region Namespaces
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace ViewPurge
{
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
            // Method to add Tab and Panel 
            RibbonPanel panel = CreateRibbonPanel(a);
            // Reflection to look for this assembly path 
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            // Add button to panel 
            PushButton button = panel.AddItem(new PushButtonData("ViewPurge", "ViewPurge", thisAssemblyPath, "ViewPurge.Command")) as PushButton;
            // Add tool tip 
            button.ToolTip = "Clean up views by selection!";

            a.ApplicationClosing += a_ApplicationClosing;

            //Set Application to Idling
            a.Idling += a_Idling;

            return Result.Succeeded;
        }
        void a_Idling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {

        }
        void a_ApplicationClosing(object sender, Autodesk.Revit.UI.Events.ApplicationClosingEventArgs e)
        {
            throw new NotImplementedException();
        }
        public RibbonPanel CreateRibbonPanel(UIControlledApplication a)
        {
            // Tab name 
            string tab = "TadTools";

            // Empty ribbon panel 
            RibbonPanel ribbonPanel = null;

            // Try to create ribbon tab. 
            try
            {
                a.CreateRibbonTab(tab);
            }
            catch { }

            // Try to create ribbon panel. 
            try
            {
                RibbonPanel panel = a.CreateRibbonPanel(tab, "Project Management");
            }
            catch { }

            // Search existing tab for your panel. 
            List<RibbonPanel> panels = a.GetRibbonPanels(tab);
            foreach (RibbonPanel p in panels)
            {
                if (p.Name == "Project Management")
                {
                    ribbonPanel = p;
                }
            }
            //return panel 
            return ribbonPanel;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
        static BitmapSource TryGetEmbeddedImage(string name)
        {
            try
            {
                Assembly a = Assembly.GetExecutingAssembly();
                Stream s = a.GetManifestResourceStream(name);
                return BitmapFrame.Create(s);
            }
            catch
            {
                return null;
            }
        }
    }
}
