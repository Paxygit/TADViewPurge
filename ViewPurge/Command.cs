#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

#endregion

namespace ViewPurge
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;


            // Access current selection
            Selection sel = uidoc.Selection;

            // Check for empty selection
            if (sel.Equals(null))
            {
                TaskDialog EmptySel = new 
                    TaskDialog("Error"){
                    MainInstruction = "No Views Selected!"
                };
                EmptySel.Show();
            }

            List<String> SelectedElementNames = new List<string>();
            string formattedElementNames = "";


            // Add all selected elements to string list
            foreach (ElementId elementid in sel.GetElementIds())
                SelectedElementNames.Add(doc.GetElement(elementid).Name);

            //Build formatted string for confirmation task dialog
            foreach (string str in SelectedElementNames)
            {
                //If first in list
                if (!formattedElementNames.Contains(","))
                    formattedElementNames = formattedElementNames + str;
                else
                    formattedElementNames = formattedElementNames + ", " + str;
            }

            // Check if active view is not selected (causes error)
            if (!sel.GetElementIds().Contains(doc.ActiveView.Id))
            {
                TaskDialog taskDialogActiveViewError = new
                TaskDialog("Error!")
                {
                    CommonButtons = TaskDialogCommonButtons.Ok,
                    MainInstruction = "You must have your active view selected!",
                    AllowCancellation = false
                };
                return Result.Cancelled;
            }

            // TaskDialog to verify before transaction
            TaskDialog taskDialog = new
                TaskDialog("ViewPurge") {
                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.Cancel,
                MainInstruction = "Are you sure you want to delete unselected views? WARNING: THIS CANNOT BE UNDONE! \n Views saved: " + formattedElementNames,
                AllowCancellation = true
            };

            // Show TaskDialog
            TaskDialogResult taskDialogResult = taskDialog.Show();
            if (taskDialogResult == TaskDialogResult.Cancel)
                return Result.Cancelled;

            // Operation is a go!
            if (taskDialogResult == TaskDialogResult.Yes)
            {
                UIApplication application = commandData.Application;
                Document document = application.ActiveUIDocument.Document;

                // Filter for all view types in project
                FilteredElementCollector viewPlans = new FilteredElementCollector(document)
                .OfClass(typeof(ViewPlan));
                FilteredElementCollector viewSections = new FilteredElementCollector(document)
                .OfClass(typeof(ViewSection));
                FilteredElementCollector viewSchedules = new FilteredElementCollector(document)
                .OfClass(typeof(ViewSchedule));
                FilteredElementCollector view3Ds = new FilteredElementCollector(document)
                .OfClass(typeof(View3D));
                FilteredElementCollector viewSheets = new FilteredElementCollector(document)
                .OfClass(typeof(ViewSheet));


                // Start tx
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("View Purge");

                    // Init Collection for deletion
                    ICollection<Autodesk.Revit.DB.ElementId> idSelection = new Collection<ElementId>();

                    // Iterate for elements that are not contained in selection
                    foreach (Element view in viewPlans)
                        if (!sel.GetElementIds().Contains(view.Id) && view.Name.ToLower() != "project view" && view.Name.ToLower() != "system browser")
                            idSelection.Add(view.Id);

                    foreach (Element view in viewSchedules)
                        if (!sel.GetElementIds().Contains(view.Id) && view.Name.ToLower() != "project view" && view.Name.ToLower() != "system browser")
                            idSelection.Add(view.Id);

                    foreach (Element view in view3Ds)
                        if (!sel.GetElementIds().Contains(view.Id) && view.Name.ToLower() != "project view" && view.Name.ToLower() != "system browser")
                            idSelection.Add(view.Id);

                    foreach (Element view in viewSheets)
                        if (!sel.GetElementIds().Contains(view.Id) && view.Name.ToLower() != "project view" && view.Name.ToLower() != "system browser")
                            idSelection.Add(view.Id);

                    foreach (Element view in viewSections)
                        if (!sel.GetElementIds().Contains(view.Id) && view.Name.ToLower() != "project view" && view.Name.ToLower() != "system browser")
                            idSelection.Add(view.Id);

                    // Try to remove all sections in Icollection
                    try
                    {
                        ICollection<Autodesk.Revit.DB.ElementId> deletedIdSet = document.Delete(idSelection); 
                        
                        if (0 == deletedIdSet.Count)
                            throw new Exception("Deleting the unselected elements in Revit failed. No items to delete found");
                    }

                    // Unknown Error
                    catch (Exception e)
                    {
                        TaskDialog taskDialogUnkError = new
                            TaskDialog("ViewPurge") {
                            MainInstruction = "An unknown error occured. Please send a screenshot to Frank Kuenneke:\n" + e.Source + "\n "+ e.Message,
                            AllowCancellation = true
                        };
                        return Result.Failed;
                    }
                   

                    TaskDialog.Show("Revit", "Unselected views have been removed.");
                    tx.Commit();
                }
            }

            else
                return Result.Succeeded;

            return Result.Succeeded;
        }

    }
}
