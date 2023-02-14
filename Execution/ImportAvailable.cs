using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BimkravRvt.Execution
{
    class ImportAvailable : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return App.ViewModel != null && App.ViewModel.SharedSettingsOK;
        }
    }
}