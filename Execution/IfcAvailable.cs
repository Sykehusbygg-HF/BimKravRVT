using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BimkravRvt.Execution
{
    class IfcAvailable : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return App.ViewModel != null && App.ViewModel.IfcSettingsOK;
        }
    }
}