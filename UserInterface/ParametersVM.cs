using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BimkravRvt.UserInterface
{
    public class ParametersVM : Utils.NotifierBase
    {
        public Document Document;
        public Application Application;
        public ObservableCollection<BimkravParameter> Parameters
        {
            get => parameters;
            private set
            {
                SetNotify(ref parameters, value);

            }
        }
        private ObservableCollection<BimkravParameter> parameters;
        public ObservableCollection<ParameterGroup> Groups { get; } = new ObservableCollection<ParameterGroup>();
        public IEnumerable<BimkravParameter> MissingParameters
        {
            get
            {
                return parameters.Where(p => p.Status == Status.Missing);
            }
        }
        public IEnumerable<BimkravParameter> UpdatingParameters
        {
            get
            {
                return parameters.Where(p => p.Status == Status.CategoriesMissing);
            }
        }
        public IEnumerable<BimkravParameter> ExistingParameters
        {
            get
            {
                return parameters.Where(p => p.Status == Status.Exists);
            }
        }


        public ParametersVM(Document doc, Application app)
        {
            Document = doc;
            Application = app;
        }

        public void AddParameters(IEnumerable<BimkravParameter> parameters)
        {
            Parameters = new ObservableCollection<BimkravParameter>(parameters);
        }

        public void AddGroups()
        {
            if (MissingParameters.ToList().Count > 0)
            {
                var missingGroup = new ParameterGroup("Missing parameters", Status.Missing, MissingParameters);
                Groups.Add(missingGroup);
            }
            if (UpdatingParameters.ToList().Count > 0)
            {
                var updatingGroup = new ParameterGroup("Incorrect category definition", Status.CategoriesMissing, UpdatingParameters);
                Groups.Add(updatingGroup);
            }
            if (ExistingParameters.ToList().Count > 0)
            {
                var existingGroup = new ParameterGroup("Existing parameters", Status.Exists, ExistingParameters);
                Groups.Add(existingGroup);
            }
        }
    }

    public class ParameterGroup : Utils.NotifierBase
    {
        public ParameterGroup(string title, Status status, IEnumerable<BimkravParameter> parameters)
        {
            this.Title = title;
            this.Status = status;
            this.BimkravParameters = new ObservableCollection<BimkravParameter>(parameters);
        }
        public string Title { get; }
        public ObservableCollection<BimkravParameter> BimkravParameters { get; set; }
        private bool? groupIsChecked;
        public Status Status { get; }
        public bool? GroupIsChecked { get => groupIsChecked; set => SetNotify(ref groupIsChecked, value); }
        public System.Windows.Visibility ShowCheckbox
        {
            get
            {
                return Status != Status.Exists ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }

        }
        public void UpdateChecked()
        {
            if (BimkravParameters.Where(p => p.IsChecked).ToList().Count == BimkravParameters.Count)
                GroupIsChecked = true;
            else if (BimkravParameters.Where(p => p.IsChecked).ToList().Count == 0)
                GroupIsChecked = false;
            else
                GroupIsChecked = null;
        }
        public void SetIsChecked(bool newValue)
        {
            foreach (var p in BimkravParameters)
            {
                p.IsChecked = newValue;
            }
        }
    }

    public class BimkravParameter : Utils.NotifierBase
    {
        public ObservableCollection<object> Values;
        public string ParameterName { get; }
        public string ParameterGroup { get; }
#if (V2020 || V2021 || V2022)
        public ParameterType ParameterType { get; }       
#else
        public ForgeTypeId ParameterType { get; }
#endif

        public string DataTypeIfc { get; }
        private Status status;
        public Status Status
        {

            get => status;
            private set
            {
                SetNotify(ref status, value);
            }
        }
        public CategorySet CategorySet { get; }
        public string IfcEntity { get; }
        public string PropertySetName { get; }
        private bool isInstance;
        public bool IsInstance
        {

            get => isInstance;
            private set
            {
                SetNotify(ref isInstance, value);
            }
        }
        public Definition Definition { get; private set; }
        public Guid GUID { get; private set; }
        public DefinitionStatus DefinitionStatus { get; private set; }
        public ElementBinding Binding { get; private set; }
        private bool isChecked;
        public bool IsChecked
        {
            get => isChecked;
            set
            {
                SetNotify(ref isChecked, value);
            }
        }
        public System.Windows.Visibility ShowCheckbox
        {
            get
            {
                return Status != Status.Exists ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }

        }


        public BimkravParameter(string name, string groupName, string type, string ifcEntity, string propertySetName, string typeInstance)
        {
            ParameterName = name;
            ParameterGroup = groupName;
            DataTypeIfc = type;
            IfcEntity = ifcEntity;
            PropertySetName = propertySetName;
            IsInstance = typeInstance == "Instans" || typeInstance.ToLower() == "instance";
        }
        public BimkravParameter(string name, string groupName, string type, CategorySet categories, string typeInstance, string guid)
        {
            ParameterName = name;
            ParameterGroup = groupName;
            ParameterType = Utils.Utils.ConvertFromIfcType(type);
            DataTypeIfc = type;
            IsChecked = true;
            CategorySet = categories;
            IsInstance = typeInstance == "Instans" || typeInstance.ToLower() == "instance";
            GUID = new Guid(guid);

            Status = Status.Undefined;
            DefinitionStatus = DefinitionStatus.Undefined;
        }

        public void CheckStatus(BindingMap bindingMap)
        {
            var iterator = bindingMap.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
#if (V2020 || V2021 || V2022)
                if (iterator.Key != null && iterator.Key.Name == ParameterName && iterator.Key.ParameterType == ParameterType)
#else
                if (iterator.Key != null && iterator.Key.Name == ParameterName && iterator.Key.GetDataType() == ParameterType)
#endif
                {
                    Binding = bindingMap.get_Item(iterator.Key) as ElementBinding;
                    if (CategorySetContainedInOther(Binding.Categories, CategorySet))
                    {
                        Status = Status.Exists;
                        break;
                    }
                    else
                    {
                        //this.Binding.Categories = CategorySet;
                        this.Binding.Categories = ExtendCategorySet(this.Binding.Categories, CategorySet);
                        Status = Status.CategoriesMissing;
                        break;
                    }
                }
                else
                {
                    Status = Status.Missing;
                }
            }
        }
        private bool CategorySetContainedInOther(CategorySet bindingCategorySet, CategorySet databaseCategorySet)
        {
            var iterator = databaseCategorySet.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                var category = iterator.Current as Category;
                if (!bindingCategorySet.Contains(category))
                    return false;
            }

            return true;
        }

        private CategorySet ExtendCategorySet(CategorySet categorySetToExtend, CategorySet extensionSet)
        {
            var iterator = extensionSet.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                var category = iterator.Current as Category;
                if (!categorySetToExtend.Contains(category))
                    categorySetToExtend.Insert(category);
            }

            return categorySetToExtend;
        }

        private bool CategorySetsAreEqual(CategorySet categorySet1, CategorySet categorySet2)
        {
            if (categorySet1.Size != categorySet2.Size)
                return false;
            var iterator = categorySet1.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                var category = iterator.Current as Category;
                if (!categorySet2.Contains(category))
                    return false;
            }

            return true;
        }

        public void InsertBinding(ElementBinding bind)
        {
            Binding = bind;
        }

        public void AssignDefinition(DefinitionFile sharedParameterFile)
        {
            try
            {
                Definition = sharedParameterFile.Groups.get_Item(ParameterGroup).Definitions.get_Item(ParameterName);
                
                if (DefinitionStatus != DefinitionStatus.Added)
                    DefinitionStatus = Definition != null ? DefinitionStatus.Existed : DefinitionStatus.Failed;
            }
            catch
            {
                DefinitionStatus = DefinitionStatus.Failed;
            }
            return;
        }
        public void CreateMissingExternalDefinitions(DefinitionFile sharedParameterFile)
        {
            var group = sharedParameterFile.Groups.get_Item(ParameterGroup);
            if (group == null)
                group = sharedParameterFile.Groups.Create(ParameterGroup);

            Definition = group.Definitions.get_Item(ParameterName);
            if (Definition == null || (Definition as ExternalDefinition)?.GUID != GUID)
            {
                var options = new ExternalDefinitionCreationOptions(ParameterName, ParameterType)
                {
                    GUID = GUID
                };
                group.Definitions.Create(options);
                DefinitionStatus = DefinitionStatus.Added;
            }
            return;
        }

        public override bool Equals(object obj)
        {
            return obj is BimkravParameter parameter &&
                   ParameterName == parameter.ParameterName &&
                   DataTypeIfc == parameter.DataTypeIfc;
        }

        public override int GetHashCode()
        {
            int hashCode = 307167150;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ParameterName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DataTypeIfc);
            return hashCode;
        }
    }

    public enum Status
    {
        Exists,
        CategoriesMissing,
        Missing,
        Undefined
    }
    public enum DefinitionStatus
    {
        Existed,
        Added,
        Failed,
        Undefined
    }
}
