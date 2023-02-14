using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimkravRvt.UserInterface;
using MySqlConnector;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Interop;
using BIMLookup.NetApi;
using BIMLookup.NetApi.Classes;
using System;
using System.Security.Principal;

namespace BimkravRvt.Execution
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    class ImportParameters : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Application app = uiapp.Application;
            Document doc = uiapp.ActiveUIDocument.Document;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            var initResult = CheckInitConditions(doc, app);
            if (initResult != Result.Succeeded)
                return initResult;

            var vm = new ParametersVM(doc, app);

            bool parametersAdded = false;

            if (App.ViewModel.Connection == Connection.MariaDB)
                parametersAdded = ImportParametersForProject_MARIADB(ref vm);
            else if (App.ViewModel.Connection == Connection.AEC)
                parametersAdded = ImportParametersForProject_AEC(ref vm);
            else if (App.ViewModel.Connection == Connection.AEC_NOAUTH)
                parametersAdded = ImportParametersForProject_AEC_NOAUTH(ref vm);

            if (!App.ViewModel.IsConnectionOK)
            {
                if (App.ViewModel.Connection != Connection.MariaDB)
                    TaskDialog.Show("Bimkrav", $"Error in importing parameters. Not able to connect to the api. Cant connect to {App.APIEndPoint}");
                else
                    TaskDialog.Show("Bimkrav", "Error in importing parameters. Not able to connect to database");
                return Result.Cancelled;
            }
            if (!parametersAdded)
            {
                TaskDialog.Show("Bimkrav", $"Error in importing parameters. Not parameters found for Discipline {App.ViewModel.Discipline}, Project {App.ViewModel.Project}, Phase {App.ViewModel.Phase} from database");
                return Result.Cancelled;
            }

            string log = AssignDefinitionStatus(ref vm);
            if(!string.IsNullOrEmpty(log))
            {
                TaskDialog.Show("AssignDefinitionStatus", $"Error: {log}");
            }

            var win = new ConfirmParameters(vm);

            _ = new WindowInteropHelper(win)
            { Owner = uidoc.Application.MainWindowHandle };
            var result = win.ShowDialog().Value;
            if (!result)
                return Result.Cancelled;

            var messageToUser = LoadParameters(vm);
            TaskDialog.Show("Sykehusbygg - Bimkrav", messageToUser);

            return Result.Succeeded;
        }
        private bool ImportParametersForProject_AEC_NOAUTH(ref ParametersVM vm)
        {
            var doc = vm.Document;
            List<BimkravParameter> parameters = new List<BimkravParameter>();
            if (App.API == null)
                App.API = new BIMLookupAPI();
            App.API.Uri = App.APIEndPoint;
            try
            {

                var sql = $"SELECT PropertyName, PropertyGroup, IfcPropertyType, GROUP_CONCAT(RevitElement), TypeInstans, PropertyGUID " +
                          $"FROM `bim`.`view_masterkrav_project` " +
                          $"WHERE {App.ViewModel.Phase} = 1 " +
                          $"AND DisciplineCode = '{App.ViewModel.Discipline.Tag}' " +
                          $"AND ProjectCode = '{App.ViewModel.Project.Tag}' " +
                          $"GROUP BY PropertyName";

                List<MasterkravProjectView> pis = App.API.GetMasterkravProjectView(App.ViewModel.Project.Tag, App.ViewModel.Phase.ToString(), App.ViewModel.Discipline.Tag, true);
                if (pis == null)
                {
                    //TODO: warn and exit
                }
                //Fulgruppering enligt sql ovan...
                var pisgrouped = pis.GroupBy(x => x.PropertyName + x.PropertyGroup + x.IfcPropertyType + x.TypeInstance + x.PropertyGuid);
                Categories categoriesAll = doc.Settings.Categories;

                foreach (var p in pisgrouped)
                {
                    CategorySet categorySet = vm.Application.Create.NewCategorySet();
                    foreach (var joindprop in p)
                    {
                        try
                        {
                            Category category = categoriesAll.get_Item(joindprop.RevitElement);
                            categorySet.Insert(category);
                        }
                        catch { }
                    }
                    var prop = p.FirstOrDefault();
                    if (prop == null)
                        continue;
                    BimkravParameter parameter = new BimkravParameter(prop.PropertyName, prop.PropertyGroup, prop.IfcPropertyType, categorySet, prop.TypeIntsnceAsString, prop.PropertyGuid);
                    parameters.Add(parameter);
                }
                vm.AddParameters(parameters);
                App.ViewModel.IsConnectionOK = true;
            }
            catch
            {
                App.ViewModel.IsConnectionOK = false;
            }
            return parameters.Count > 0;
        }
        private bool ImportParametersForProject_AEC(ref ParametersVM vm)
        {
            var doc = vm.Document;
            List<BimkravParameter> parameters = new List<BimkravParameter>();
            try
            {
                if (App.API == null)
                    App.API = new BIMLookupAPI();
                App.API.Uri = App.APIEndPoint;
                App.API.UserName = App.APIUserName;
                App.API.Password = App.APIPassword;

                List<PropertyInstance> pis = App.API.GetPropertyInstances(App.ViewModel.Project.Tag, App.ViewModel.Phase.ToString(), App.ViewModel.Discipline.Tag);

                //TODO: SQL View instead for the api
                Categories categoriesAll = doc.Settings.Categories;



                foreach (PropertyInstance pi in pis)
                {
                    CategorySet categorySet = vm.Application.Create.NewCategorySet();
                    foreach (string cat in pi.Categories)
                    {
                        try
                        {
                            Category category = categoriesAll.get_Item(cat);
                            categorySet.Insert(category);
                        }
                        catch { }
                    }
                    BimkravParameter parameter = new BimkravParameter(pi.Name, pi.PropertyGroupName, pi.RevitPropertyTypeName, categorySet, pi.Type_InstanceName, pi.Guid);
                    parameters.Add(parameter);
                }

                vm.AddParameters(parameters);
                App.ViewModel.IsConnectionOK = true;

            }
            catch
            {
                App.ViewModel.IsConnectionOK = false;
            }

            return parameters.Count > 0;
        }
        private bool ImportParametersForProject_MARIADB(ref ParametersVM vm)
        {
            var doc = vm.Document;
            List<BimkravParameter> parameters = new List<BimkravParameter>();

            try
            {
                using (var cnn = new MySqlConnection(App.ViewModel.ConnectionString))
                {
                    var sql = $"SELECT PropertyName, PropertyGroup, IfcPropertyType, GROUP_CONCAT(RevitElement), TypeInstans, PropertyGUID " +
                              $"FROM `bim`.`view_masterkrav_project` " +
                              $"WHERE {App.ViewModel.Phase} = 1 " +
                              $"AND DisciplineCode = '{App.ViewModel.Discipline.Tag}' " +
                              $"AND ProjectCode = '{App.ViewModel.Project.Tag}' " +
                              $"GROUP BY PropertyName";
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


                    for (int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        var items = dataTable.Rows[i].ItemArray;
                        CategorySet categorySet = vm.Application.Create.NewCategorySet();

                        foreach (string cat in items[3].ToString().Split(','))
                        {
                            try
                            {
                                Category category = categoriesAll.get_Item(cat);
                                categorySet.Insert(category);
                            }
                            catch { }
                        }

                        BimkravParameter parameter = new BimkravParameter(items[0].ToString(), items[1].ToString(), items[2].ToString(), categorySet, items[4].ToString(), items[5].ToString());
                        parameters.Add(parameter);
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
        private string LoadParameters(ParametersVM vm)
        {
            var doc = vm.Document;
            var app = vm.Application;

            int miss, catmiss;
            miss = catmiss = 0;

            using (Transaction transaction = new Transaction(doc))
            {
                transaction.Start("Import parameters from Bimkrav");
                foreach (var para in vm.Parameters.Where(p => (p.Status == Status.CategoriesMissing || p.Status == Status.Missing) && p.IsChecked))
                {
                    switch (para.Status)
                    {
                        case Status.Missing:
                            var binding = para.IsInstance ? app.Create.NewInstanceBinding(para.CategorySet) : app.Create.NewTypeBinding(para.CategorySet) as ElementBinding;
                            para.InsertBinding(binding);
                            AddParameter(doc, para);
                            miss++;
                            break;
                        case Status.CategoriesMissing:
                            UpdateCategories(doc, para);
                            catmiss++;
                            break;
                        default:
                            break;
                    }
                }
                transaction.Commit();
            }

            return $"{miss} parameters added, {catmiss} parameters updated.";
        }

        private string AssignDefinitionStatus(ref ParametersVM vm)
        {
            string message = string.Empty;
            var definitionFile = vm.Application.OpenSharedParameterFile();

            foreach (var para in vm.Parameters)
            {
                System.Diagnostics.Debug.Print($"AssignDefinitionStatus: {para.ParameterName}");
                try
                {
                    para.CreateMissingExternalDefinitions(definitionFile);
                    para.AssignDefinition(definitionFile);
                }
                catch (Exception ex)
                {
                    message += $"{para.ParameterName}: {ex.Message}\r\n";
                }
            }

            var bindings = vm.Document.ParameterBindings;
            foreach (var para in vm.Parameters)
            {
                para.CheckStatus(bindings);
            }
            vm.AddGroups();
            return message;
        }

        private Result CheckInitConditions(Document doc, Application app)
        {
            string sharedParameterFile = doc.ProjectInformation.get_Parameter(Utils.Utils.GetSharedParameterGUID()).AsString();
            int phaseInt = doc.ProjectInformation.get_Parameter(Utils.Utils.GetPhaseParameterGUID()).AsInteger();

            if (string.IsNullOrEmpty(sharedParameterFile) || !System.IO.File.Exists(sharedParameterFile))
            {
                TaskDialog.Show("Bimkrav", "The shared parameter file given in the settings does not exist. Please set a valid file path.");
                return Result.Cancelled;
            }

            if (app.SharedParametersFilename != sharedParameterFile)
            {
                app.SharedParametersFilename = sharedParameterFile;
                TaskDialog.Show("Sykehusbygg - Bimkrav", "According to the Bimkrav-settings the shared parameter file was switched to " + sharedParameterFile);
            }

            var definitionFile = app.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                TaskDialog.Show("Sykehusbygg - Bimkrav", "The given shared parameter file in the Bimkrav-settings is not a valid, or it is missing");
                return Result.Cancelled;
            }

            if (phaseInt < 100)
            {
                TaskDialog.Show("Bimkrav", "Please set a phase in the Settings-window");
                return Result.Cancelled;
            }
            return Result.Succeeded;
        }

        private void UpdateCategories(Document doc, BimkravParameter parameter)
        {
            doc.ParameterBindings.ReInsert(parameter.Definition, parameter.Binding, BuiltInParameterGroup.PG_IFC);
        }

        private void AddParameter(Document doc, BimkravParameter parameter)
        {
            doc.ParameterBindings.Insert(parameter.Definition, parameter.Binding, BuiltInParameterGroup.PG_IFC);
        }
    }
}
