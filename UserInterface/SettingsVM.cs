using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MySqlConnector;
using System.Data;
using BIMLookup.NetApi;
using BIMLookup.NetApi.Classes;

namespace BimkravRvt.UserInterface
{
    public class SettingsVM : Utils.NotifierBase
    {
        private string sharedFilePath;
        private string iFCSetupFilePath;
        private Phase phase;
        public Document Doc { get; }
        public ObservableCollection<Connection> Connections { get; } = new ObservableCollection<Connection>();
        public Connection Connection { get; set; } = Connection.AEC_NOAUTH;
        public string ConnectionString { get { return $"host=sql-bimkrav-prod.mariadb.database.azure.com;user=Prosjekterende@sql-bimkrav-prod;password=;database=bim"; } }
        public ObservableCollection<ProjectDiscipline> Disciplines { get; } = new ObservableCollection<ProjectDiscipline>();
        public ObservableCollection<ProjectDiscipline> Projects { get; } = new ObservableCollection<ProjectDiscipline>();
        private ProjectDiscipline project;
        public ProjectDiscipline Project { get => project; set => SetNotify(ref project, value); }
        private ProjectDiscipline discipline;
        public ProjectDiscipline Discipline { get => discipline; set => SetNotify(ref discipline, value); }
        public string SharedFilePath { get => sharedFilePath; set => SetNotify(ref sharedFilePath, value); }
        public string IFCSetupFilePath { get => iFCSetupFilePath; set => SetNotify(ref iFCSetupFilePath, value); }
        public Phase Phase { get => phase; set => SetNotify(ref phase, value); }
        public ObservableCollection<BimkravParameter> Parameters;
        public bool IfcSettingsOK;
        public bool SharedSettingsOK;
        public bool IsConnectionOK;

        public SettingsVM(Document doc)
        {
            IsConnectionOK = true;

            if (this.Connection == Connection.MariaDB)
                TestConnection_MARIADB();
            else if (this.Connection == Connection.AEC)
                TestConnection_AEC();
            else if (this.Connection == Connection.AEC_NOAUTH)
                TestConnection_AEC_NOAUTH();

            if (IsConnectionOK)
            {
                Doc = doc;
                if (this.Connection == Connection.MariaDB)
                {
                    GetProjects_MARIADB();
                    GetDisciplines_MARIADB();
                }
                else if (this.Connection == Connection.AEC)
                {
                    GetProjects_AEC();
                    GetDisciplines_AEC();
                }
                else if (this.Connection == Connection.AEC_NOAUTH)
                {
                    GetProjects_AEC_NOAUTH();
                    GetDisciplines_AEC_NOAUTH();
                }
                GetSettings();
            }
            Connections.Add(Connection.MariaDB);
            Connections.Add(Connection.AEC);
            Connections.Add(Connection.AEC_NOAUTH);


        }
        private void TestConnection_AEC_NOAUTH()
        {
            if (App.API == null)
                App.API = new BIMLookupAPI();
            App.API.Uri = App.APIEndPoint;

            //string token = api.GetCurrentToken();  
            IsConnectionOK = App.API.ConnectionNoAuthOK();
        }
        private void GetProjects_AEC_NOAUTH()
        {
            var sql = $"SELECT ProjectName, ProjectCode " +
                      $"FROM `bim`.`tblproject` ";
            if (App.API == null)
                App.API = new BIMLookupAPI();
            App.API.Uri = App.APIEndPoint;
            if (App.API.ConnectionNoAuthOK())
            {
                var projects = App.API.GetAllProjectsNoAuth();
                if (projects != null)
                {
                    foreach (Project project in projects)
                    {
                        Projects.Add(new ProjectDiscipline(project.Name, project.Code));
                    }
                }
            }
        }
        private void GetDisciplines_AEC_NOAUTH()
        {
            var sql = $"SELECT DisciplineName, DisciplineCode " +
                      $"FROM `bim`.`tbldiscipline` ";
            if (App.API == null)
                App.API = new BIMLookupAPI();
            App.API.Uri = App.APIEndPoint;
            if (App.API.ConnectionNoAuthOK())
            {
                var disciplines = App.API.GetAllDisciplinesNoAuth();
                if (disciplines != null)
                {
                    foreach (Discipline discipline in disciplines)
                    {
                        Disciplines.Add(new ProjectDiscipline(discipline.Name, discipline.Code));
                    }
                }
            }
        }
        private void TestConnection_AEC()
        {
            if (App.API == null)
                App.API = new BIMLookupAPI();
            App.API.Uri = App.APIEndPoint;
            App.API.UserName = App.APIUserName;
            App.API.Password = App.APIPassword;
            //string token = api.GetCurrentToken();  
            IsConnectionOK = App.API.ConnectionOK();
        }
        private void GetProjects_AEC()
        {
            var sql = $"SELECT ProjectName, ProjectCode " +
                      $"FROM `bim`.`tblproject` ";
            if (App.API == null)
                App.API = new BIMLookupAPI();
            App.API.Uri = App.APIEndPoint;
            App.API.UserName = App.APIUserName;
            App.API.Password = App.APIPassword;
            if (App.API.ConnectionOK())
            {
                var projects = App.API.GetAllProjects();
                if (projects != null)
                {
                    foreach (Project project in projects)
                    {
                        Projects.Add(new ProjectDiscipline(project.Name, project.Code));
                    }
                }
            }
        }
        private void GetDisciplines_AEC()
        {
            var sql = $"SELECT DisciplineName, DisciplineCode " +
                      $"FROM `bim`.`tbldiscipline` ";
            if (App.API == null)
                App.API = new BIMLookupAPI();
            App.API.Uri = App.APIEndPoint;
            App.API.UserName = App.APIUserName;
            App.API.Password = App.APIPassword; ;
            if (App.API.ConnectionOK())
            {
                var disciplines = App.API.GetAllDisciplines();
                if (disciplines != null)
                {
                    foreach (Discipline discipline in disciplines)
                    {
                        Disciplines.Add(new ProjectDiscipline(discipline.Name, discipline.Code));
                    }
                }
            }
        }
        private void TestConnection_MARIADB()
        {
            var sql = $"SELECT ProjectName, ProjectCode " +
                      $"FROM `bim`.`tblproject` ";

            try
            {
                using (MySqlConnection cnn = new MySqlConnection(ConnectionString))
                {
                    MySqlCommand command = new MySqlCommand(sql, cnn);
                    DataSet sqlDataSet = new DataSet();

                    cnn.Open();

                    using (var mysqldata = new MySqlDataAdapter(sql, cnn))
                    {
                        mysqldata.InsertCommand = new MySqlCommand(sql, cnn);
                        mysqldata.InsertCommand.ExecuteNonQuery();
                        mysqldata.Fill(sqlDataSet, "result");
                    }

                    cnn.Close();
                }
                IsConnectionOK = true;
            }
            catch
            {
                IsConnectionOK = false;
            }
        }
        private void GetProjects_MARIADB()
        {
            var sql = $"SELECT ProjectName, ProjectCode " +
                      $"FROM `bim`.`tblproject` ";

            using (MySqlConnection cnn = new MySqlConnection(ConnectionString))
            {
                MySqlCommand command = new MySqlCommand(sql, cnn);
                DataSet sqlDataSet = new DataSet();

                cnn.Open();

                using (var mysqldata = new MySqlDataAdapter(sql, cnn))
                {
                    mysqldata.InsertCommand = new MySqlCommand(sql, cnn);
                    mysqldata.InsertCommand.ExecuteNonQuery();
                    mysqldata.Fill(sqlDataSet, "result");
                }

                var dataTable = sqlDataSet.Tables[0];

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    var items = dataTable.Rows[i].ItemArray;
                    Projects.Add(new ProjectDiscipline(items[0].ToString(), items[1].ToString()));
                }

                cnn.Close();
            }
        }

        private void GetDisciplines_MARIADB()
        {
            var sql = $"SELECT DisciplineName, DisciplineCode " +
                      $"FROM `bim`.`tbldiscipline` ";

            using (MySqlConnection cnn = new MySqlConnection(ConnectionString))
            {
                MySqlCommand command = new MySqlCommand(sql, cnn);
                DataSet sqlDataSet = new DataSet();

                cnn.Open();

                using (var mysqldata = new MySqlDataAdapter(sql, cnn))
                {
                    mysqldata.InsertCommand = new MySqlCommand(sql, cnn);
                    mysqldata.InsertCommand.ExecuteNonQuery();
                    mysqldata.Fill(sqlDataSet, "result");
                }

                var dataTable = sqlDataSet.Tables[0];

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    var items = dataTable.Rows[i].ItemArray;
                    Disciplines.Add(new ProjectDiscipline(items[0].ToString(), items[1].ToString()));
                }

                cnn.Close();
            }
        }
        private void GetSettings()
        {
            var pi = Doc.ProjectInformation;

            var sharedPara = pi.get_Parameter(Utils.Utils.GetProjectParameterGUID());
            if (sharedPara == null)
                AddAddinSharedParameters(new List<string>() { "BimkravSharedParameterFilePath", "BimkravIFCExportSetupFilePath", "BimkravPhase", "BimkravProject", "BimkravDiscipline" });

            IFCSetupFilePath = "";
            SharedFilePath = "";
            Phase = (Phase)100;

            if (!string.IsNullOrEmpty(pi.get_Parameter(Utils.Utils.GetIFCParameterGUID()).AsString()))
                IFCSetupFilePath = pi.get_Parameter(Utils.Utils.GetIFCParameterGUID()).AsString();

            if (!string.IsNullOrEmpty(pi.get_Parameter(Utils.Utils.GetSharedParameterGUID()).AsString()))
                SharedFilePath = pi.get_Parameter(Utils.Utils.GetSharedParameterGUID()).AsString();

            if (pi.get_Parameter(Utils.Utils.GetPhaseParameterGUID()).AsInteger() > 100)
                Phase = (Phase)pi.get_Parameter(Utils.Utils.GetPhaseParameterGUID()).AsInteger();

            if (!string.IsNullOrEmpty(pi.get_Parameter(Utils.Utils.GetProjectParameterGUID()).AsString()))
            {
                var tag = pi.get_Parameter(Utils.Utils.GetProjectParameterGUID()).AsString();
                Project = Projects.FirstOrDefault(p => p.Tag == tag) ?? Projects.First();
            }
            else
            {
                Project = Projects.First();
            }

            if (!string.IsNullOrEmpty(pi.get_Parameter(Utils.Utils.GetDisciplineParameterGUID()).AsString()))
            {
                var tag = pi.get_Parameter(Utils.Utils.GetDisciplineParameterGUID()).AsString();
                Discipline = Disciplines.FirstOrDefault(p => p.Tag == tag) ?? Disciplines.First();
            }
            else
            {
                Discipline = Disciplines.First();
            }
        }

        private void AddAddinSharedParameters(IEnumerable<string> parameterNames)
        {
            string oldSharedParameterFile = null;
            if (App.uiapp.ControlledApplication.SharedParametersFilename == null || App.uiapp.ControlledApplication.OpenSharedParameterFile() == null || App.uiapp.ControlledApplication.OpenSharedParameterFile().Groups.get_Item("BimkravAddin") == null)
            {
                var rootDir = System.IO.Path.GetDirectoryName(App.assemblyPath);
                var fileName = System.IO.Path.Combine(rootDir, "Bimkrav.txt");
                oldSharedParameterFile = App.uiapp.ControlledApplication.SharedParametersFilename;
                App.uiapp.ControlledApplication.SharedParametersFilename = fileName;
            }

            DefinitionGroup group = App.uiapp.ControlledApplication.OpenSharedParameterFile().Groups.get_Item("BimkravAddin");

            List<ExternalDefinition> definitions = new List<ExternalDefinition>();
            foreach (var name in parameterNames)
            {
                ExternalDefinition definition = group.Definitions.get_Item(name) as ExternalDefinition;
                definitions.Add(definition);
            }


            CategorySet categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(Doc, BuiltInCategory.OST_ProjectInformation));
            InstanceBinding binding = new InstanceBinding(categorySet);

            using (Transaction transaction = new Transaction(Doc))
            {
                transaction.Start("Import addin parameters");
                foreach (var definition in definitions)
                {
                    if (!Doc.ParameterBindings.Contains(definition))
                        Doc.ParameterBindings.Insert(definition, binding, BuiltInParameterGroup.PG_GENERAL);
                }
                transaction.Commit();
            }

            if (oldSharedParameterFile != null)
                App.uiapp.ControlledApplication.SharedParametersFilename = oldSharedParameterFile;
        }
    }

    public enum Phase
    {
        Skisseprosjekt = 100,
        Forprosjekt = 200,
        Detaljprosjekt = 300,
        Arbeidstegning = 400,
        Overlevering = 500
    }
    public enum Connection
    {
        MariaDB,
        AEC_NOAUTH,
        AEC
    }
    public static class PhaseList
    {
        public static IEnumerable<Phase> GetEnumTypes => Enum.GetValues(typeof(Phase)).Cast<Phase>();
    }
}
