using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimkravRvt.UserInterface;
using MySqlConnector;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using BIMLookup.NetApi;
using BIMLookup.NetApi.Classes;
using System.Diagnostics;

namespace BimkravRvt.Execution
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    class CreateIFCSetup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Application app = uiapp.Application;
            Document doc = uiapp.ActiveUIDocument.Document;
            
            var initResult = CheckInitConditions(doc);
            if (initResult != Result.Succeeded)
                return initResult;

            var vm = new ParametersVM(doc, app);
            bool parametersAdded = false;
            try
            {
                if (App.ViewModel.Connection == Connection.MariaDB)
                    parametersAdded = ImportParametersForIFCSetup_MARIADB(ref vm);
                else if (App.ViewModel.Connection == Connection.AEC)
                    parametersAdded = ImportParametersForIFCSetup_AEC(ref vm);
                else if (App.ViewModel.Connection == Connection.AEC_NOAUTH)
                    parametersAdded = ImportParametersForIFCSetup_AEC_NOAUTH(ref vm);
            }
            catch(Exception ex)
            {
                TaskDialog.Show("Bimkrav", $"Error in importing parameters. {ex.Message}");
                //App.ViewModel.TestConnection();
            }
            if (!App.ViewModel.IsConnectionOK)
            {
#if AEC
                TaskDialog.Show("Bimkrav", $"Error in importing parameters. Not able to connect to the api. Cant connect to {App.APIEndPoint}");

#else
                TaskDialog.Show("Bimkrav", "Error in importing parameters. Not able to connect to database");
#endif


                return Result.Cancelled;
            }
            if (!parametersAdded)
            {
                TaskDialog.Show("Bimkrav", $"Error in importing parameters. Not parameters found for Discipline {App.ViewModel.Discipline}, Project {App.ViewModel.Project}, Phase {App.ViewModel.Phase} from database");
                return Result.Cancelled;
            }

            var customSets = CreateCustomPsets(vm);
            WriteFile(doc, customSets);

            return Result.Succeeded;
        }

        private Dictionary<string, string> GetIfcMap(Application app)
        {
            var ifcTablePath = app.ExportIFCCategoryTable;
            if (string.IsNullOrWhiteSpace(ifcTablePath))
                throw new System.Exception($"The path to the IFC entity mappings is empty. Please check the IFC Options from Revit's 'File > Export > Options' menu");
            var ifcTable = System.IO.File.ReadAllLines(ifcTablePath).Where(line => !line.StartsWith("#"));

            var ifcClassMap = new Dictionary<string, string>();
            foreach (var line in ifcTable)
            {
                var cells = line.Split('\t');
                var key = string.IsNullOrEmpty(cells[1]) ? cells[0] : cells[0] + "," + cells[1];
                var value = string.IsNullOrEmpty(cells[2]) ? null : cells[2];
                if (value != null)
                    ifcClassMap.Add(key, value);
            }

            return ifcClassMap;
        }
        private string GetIfcEntity(Dictionary<string, string> ifcMap, Category category)
        {
            string ifcClass;
            if (category.Parent != null)
            {
                if (ifcMap.TryGetValue(category.Parent.Name + "," + category.Name, out ifcClass))
                    return ifcClass;
            }
            if (ifcMap.TryGetValue(category.Name, out ifcClass))
                return ifcClass;
            else
                return null;
        }
private bool ImportParametersForIFCSetup_AEC_NOAUTH(ref ParametersVM vm)
        {
            var doc = vm.Document;
            List<BimkravParameter> parameters = new List<BimkravParameter>();

            try
            {
                if(App.API == null)
                    App.API = new BIMLookupAPI(App.APIEndPoint);


                string sql = $"SELECT PropertyName, PropertyGroup, IfcPropertyType, RevitElement, PsetName, TypeInstans " +
                              $"FROM `bim`.`view_masterkrav_project` " +
                              $"WHERE {App.ViewModel.Phase} = 1 " +
                              $"AND DisciplineCode = '{App.ViewModel.Discipline.Tag}' " +
                              $"AND ProjectCode = '{App.ViewModel.Project.Tag}' ";

                List<MasterkravProjectView> pis = App.API.GetMasterkravProjectView(App.ViewModel.Project.Tag, App.ViewModel.Phase.ToString(), App.ViewModel.Discipline.Tag, true);
                if(pis == null)
                {
                    //TODO: warn and exit
                }

                //TODO: SQL View instead for the api
                Categories categoriesAll = doc.Settings.Categories;
                Dictionary<string, string> ifcMap = new Dictionary<string, string>();
                try
                {
                    ifcMap = GetIfcMap(vm.Application);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                foreach (MasterkravProjectView pi in pis)
                {

                    try
                    {
                        Category category = categoriesAll.get_Item(pi.RevitElement);
                        var ifcEntity = GetIfcEntity(ifcMap, category);
                        if (ifcEntity == null)
                            continue;
                        BimkravParameter parameter = new BimkravParameter(pi.PropertyName, pi.PropertyGroup, pi.IfcPropertyType, ifcEntity, pi.PSetName, pi.TypeIntsnceAsString);
                        parameters.Add(parameter);
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine($"Unsupported element?: {pi.RevitElement}");
                    }
                    
                }
                vm.AddParameters(parameters);
                App.ViewModel.IsConnectionOK = true;

            }
            catch(Exception ex)
            {
                App.ViewModel.IsConnectionOK = false;
                throw;
            }
            return parameters.Count > 0;
        }

        private bool ImportParametersForIFCSetup_AEC(ref ParametersVM vm)
        {
            var doc = vm.Document;
            List<BimkravParameter> parameters = new List<BimkravParameter>();

            try
            {
                if(App.API == null)
                    App.API = new BIMLookupAPI(App.APIEndPoint, App.APIUserName, App.APIPassword);


                string sql = $"SELECT PropertyName, PropertyGroup, IfcPropertyType, RevitElement, PsetName, TypeInstans " +
                              $"FROM `bim`.`view_masterkrav_project` " +
                              $"WHERE {App.ViewModel.Phase} = 1 " +
                              $"AND DisciplineCode = '{App.ViewModel.Discipline.Tag}' " +
                              $"AND ProjectCode = '{App.ViewModel.Project.Tag}' ";

                List<PropertyInstance> pis = App.API.GetPropertyInstances(App.ViewModel.Project.Tag, App.ViewModel.Phase.ToString(), App.ViewModel.Discipline.Tag);

                //TODO: SQL View instead for the api
                Categories categoriesAll = doc.Settings.Categories;
                Dictionary<string, string> ifcMap = new Dictionary<string, string>();
                try
                {
                    ifcMap = GetIfcMap(vm.Application);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                foreach (PropertyInstance pi in pis)
                {
                    foreach (string cat in pi.Categories)
                    {
                        Category category = categoriesAll.get_Item(cat);
                        var ifcEntity = GetIfcEntity(ifcMap, category);
                        if (ifcEntity == null)
                            continue;
                        BimkravParameter parameter = new BimkravParameter(pi.Name, pi.PropertyGroupName, pi.IfcPropertyType, ifcEntity,pi.PropertySetDisplayValue ,pi.Type_InstanceName);
                        parameters.Add(parameter);
                    }
                }
                vm.AddParameters(parameters);
                App.ViewModel.IsConnectionOK = true;

            }
            catch(Exception ex)
            {
                App.ViewModel.IsConnectionOK = false;
                throw;
            }
            return parameters.Count > 0;
        }
        private bool ImportParametersForIFCSetup_MARIADB(ref ParametersVM vm)
        {
            var doc = vm.Document;
            List<BimkravParameter> parameters = new List<BimkravParameter>();

            try
            {
                using (var cnn = new MySqlConnection(App.ViewModel.ConnectionString))
                {
                
                    var sql = $"SELECT PropertyName, PropertyGroup, IfcPropertyType, RevitElement, PsetName, TypeInstans " +
                              $"FROM `bim`.`view_masterkrav_project` " +
                              $"WHERE {App.ViewModel.Phase} = 1 " +
                              $"AND DisciplineCode = '{App.ViewModel.Discipline.Tag}' " +
                              $"AND ProjectCode = '{App.ViewModel.Project.Tag}' ";
                    DataSet sqlDataSet = new DataSet();

                    cnn.Open();
                    using (var mysqldata = new MySqlDataAdapter(sql, cnn))
                    {
                        mysqldata.InsertCommand = new MySqlCommand(sql, cnn);
                        mysqldata.InsertCommand.ExecuteNonQuery();
                        mysqldata.Fill(sqlDataSet, "result");
                    }

                    var dataTable = sqlDataSet.Tables[0];

                    Categories categoriesAll = doc.Settings.Categories;
                    var ifcMap = GetIfcMap(vm.Application);

                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        var items = dataTable.Rows[i].ItemArray;
                        try
                        {
                            Category category = categoriesAll.get_Item(items[3].ToString());
                            var ifcEntity = GetIfcEntity(ifcMap, category);
                            if (ifcEntity == null)
                                continue;
                            BimkravParameter parameter = new BimkravParameter(items[0].ToString(), items[1].ToString(), items[2].ToString(), ifcEntity, items[4].ToString(), items[5].ToString());
                            parameters.Add(parameter);
                        }
                        catch { }
                    }

                    cnn.Close();

                    vm.AddParameters(parameters);
                    App.ViewModel.IsConnectionOK = true;
                }
            }
            catch
            {
                App.ViewModel.IsConnectionOK = false;
            }
            return parameters.Count > 0;
        }
        private void WriteFile(Document doc, List<string> customSets)
        {
            string filePath = doc.ProjectInformation.get_Parameter(Utils.Utils.GetIFCParameterGUID()).AsString();
            bool existingContent = System.IO.File.ReadAllLines(filePath).Where(line => !(line.StartsWith("#") || string.IsNullOrEmpty(line.Trim()))).ToList().Count > 0;

            if (existingContent)
            {
                using (TaskDialog mainDialog = new TaskDialog("Bimkrav"))
                {
                    mainDialog.MainInstruction = $"{filePath} already contains information. Do you want to overwrite it?";

                    mainDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Overwrite");
                    mainDialog.CommonButtons = TaskDialogCommonButtons.Cancel;
                    mainDialog.DefaultButton = TaskDialogResult.Cancel;

                    TaskDialogResult tResult = mainDialog.Show();
                    if (tResult != TaskDialogResult.CommandLink1)
                        return;
                }
            }

            var fileContent = Utils.Utils.IfcHeader.Split(new string[] { "\r\n" }, StringSplitOptions.None).ToList();
            fileContent.AddRange(customSets);
            System.IO.File.WriteAllLines(filePath, fileContent);

            return;
        }

        private List<string> CreateCustomPsets(ParametersVM vm)
        {
            List<string> customSets = new List<string>();

            var psets = vm.Parameters.GroupBy(p => p.PropertySetName);
            
            foreach (var pset in psets)
            {
                foreach (var itdef in pset.GroupBy(ps => ps.IsInstance))
                {
                    var it = itdef.Key? "I" : "T";
                    customSets.Add("");
                    var ifcElements = string.Join(",", new HashSet<string>(itdef.Select(p => p.IfcEntity).Where(i => i.StartsWith("Ifc")).OrderBy(s => s)));
                    customSets.Add($"PropertySet:\t{pset.Key}\t{it}\t{ifcElements}");
                    foreach (var property in itdef.Distinct(new IfcPropertyEqualityComparer()))
                        customSets.Add($"\t{property.ParameterName}\t{property.DataTypeIfc}");
                }
            }

            return customSets.Skip(1).ToList();
        }

        private Result CheckInitConditions(Document doc)
        {
            string ifcFile = doc.ProjectInformation.get_Parameter(Utils.Utils.GetIFCParameterGUID()).AsString();

            if (string.IsNullOrEmpty(ifcFile) || !System.IO.File.Exists(ifcFile))
            {
                TaskDialog.Show("Bimkrav", "The custom parameter set file given in the settings does not exist. Please set a valid file path.");
                return Result.Cancelled;
            }

            return Result.Succeeded;
        }
    }

    class IfcPropertyEqualityComparer : IEqualityComparer<BimkravParameter>
    {
        public bool Equals(BimkravParameter x, BimkravParameter y)
        {
            return x.ParameterName == y.ParameterName && x.DataTypeIfc == y.DataTypeIfc;
        }

        public int GetHashCode(BimkravParameter bimkravParameter)
        {
            return base.GetHashCode();
        }
    }
}
