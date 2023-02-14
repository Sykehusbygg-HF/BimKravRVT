using Autodesk.Revit.DB;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using BimkravRvt.Utils;
using BimkravRvt.Execution;

namespace BimkravRvt.UserInterface
{
    /// <summary>
    /// Interaction logic for SettingsWin.xaml
    /// </summary>
    public partial class SettingsWin : Window
    {
        public SettingsVM ViewModel { get { return DataContext as SettingsVM; } }
        public SettingsWin()
        {
            InitializeComponent();
            this.ConnectionCombo.Visibility = System.Windows.Visibility.Hidden;
            this.ConnectionLabel.Visibility = System.Windows.Visibility.Hidden;
#if DEBUG
            this.ConnectionCombo.Visibility = System.Windows.Visibility.Visible;
            this.ConnectionLabel.Visibility = System.Windows.Visibility.Visible;


#endif
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using (Transaction transaction = new Transaction(ViewModel.Doc))
            {
                transaction.Start("Save settings");
                ViewModel.Doc.ProjectInformation.get_Parameter(Utils.Utils.GetSharedParameterGUID()).Set(ViewModel.SharedFilePath);
                ViewModel.Doc.ProjectInformation.get_Parameter(Utils.Utils.GetIFCParameterGUID()).Set(ViewModel.IFCSetupFilePath);
                ViewModel.Doc.ProjectInformation.get_Parameter(Utils.Utils.GetPhaseParameterGUID()).Set((int)ViewModel.Phase);
                ViewModel.Doc.ProjectInformation.get_Parameter(Utils.Utils.GetProjectParameterGUID()).Set(ViewModel.Project.Tag);
                ViewModel.Doc.ProjectInformation.get_Parameter(Utils.Utils.GetDisciplineParameterGUID()).Set(ViewModel.Discipline.Tag);
                transaction.Commit();
            }

            SettingsBimkrav.CheckIfcSettings(ViewModel.Doc);
            SettingsBimkrav.CheckSharedSettings(ViewModel.Doc);

            DialogResult = true;
            Close();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            bool isShared = button.Name.Contains("Shared");
            bool isSave = button.Name.Contains("Create");
            var initPath = GetInitPath(isShared);

            OpenFileDialog openFileDialog = GetDialog(isSave, isShared, initPath) as OpenFileDialog;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true && System.IO.File.Exists(openFileDialog.FileName))
            {
                SetFilePath(isShared, openFileDialog.FileName);
            }
        }

        private void SetFilePath(bool isShared, string filePath)
        {
            if (isShared)
                ViewModel.SharedFilePath = filePath;
            else
                ViewModel.IFCSetupFilePath = filePath;
        }

        private FileDialog GetDialog(bool isSave, bool isShared, string initPath)
        {
            FileDialog dialog;
            if (isSave)
                dialog = new SaveFileDialog();
            else
                dialog = new OpenFileDialog();

            string filter;
            if (isShared)
                filter = "Shared Parameter Files (*.txt)|*.txt|All files (*.*)|*.*";
            else
                filter = "Custom Parameter Sets (*.txt)|*.txt|All files (*.*)|*.*";
            dialog.Filter = filter;

            if (!string.IsNullOrEmpty(initPath))
                dialog.InitialDirectory = initPath;

            return dialog;
        }

        private string GetInitPath(bool isShared)
        {
            string initPath = "";
            string path;
            if (isShared)
                path = ViewModel.SharedFilePath;
            else
                path = ViewModel.IFCSetupFilePath;

            if (string.IsNullOrEmpty(path))
                return initPath;

            if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                initPath = System.IO.Path.GetDirectoryName(path);

            return initPath;
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {

            Button button = (Button)sender;
            bool isShared = button.Name.Contains("Shared");
            bool isSave = button.Name.Contains("Create");
            var initPath = GetInitPath(isShared);

            SaveFileDialog saveFileDialog = GetDialog(isSave, isShared, initPath) as SaveFileDialog;

            if (saveFileDialog.ShowDialog() == true)
            {
                WriteFileContent(isShared, saveFileDialog.FileName);
                SetFilePath(isShared, saveFileDialog.FileName);
            }
        }

        private void WriteFileContent(bool isShared, string filePath)
        {
            string fileContent;
            if (isShared)
                fileContent = Utils.Utils.SharedHeader;
            else
                fileContent = Utils.Utils.IfcHeader;

            System.IO.File.WriteAllText(filePath, fileContent);
        }
    }
}
