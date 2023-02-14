using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BimkravRvt.UserInterface
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ConfirmParameters : Window
    {
        public ParametersVM ViewModel { get { return DataContext as ParametersVM; } }
        public ConfirmParameters(ParametersVM vm)
        {
            InitializeComponent();
            DataContext = vm;

            Utils.Utils.CheckBounds(this);
            trvParameters.ItemsSource = ViewModel.Groups;

            foreach (var p in ViewModel.Groups)
            {
                p.UpdateChecked();
            }

        }

        private void CheckBoxGroup_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            bool newVal = (checkBox.IsChecked == true);

            ParameterGroup group = checkBox.DataContext as ParameterGroup;
            group.GroupIsChecked = newVal;
            group.SetIsChecked(newVal);
        }

        private void SglCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            BimkravParameter bkpara = checkBox.DataContext as BimkravParameter;
            var para = ViewModel.Groups.Where(p => p.Status == bkpara.Status).FirstOrDefault();
            para.UpdateChecked();
        }
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //TODO Hit Space to check/uncheck parameter, probably needs DataGrid_GotFocus-stuff.
            if (e.Key == Key.Space)
                return;
        }
    }

}
