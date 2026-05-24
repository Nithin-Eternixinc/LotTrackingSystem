
using System.Windows;
using LTS.UI.ViewModels;


namespace LTS.UI.Views
{
    /// <summary>
    /// Interaction logic for RoutePickerWindow.xaml
    /// </summary>
    public partial class RoutePickerWindow : Window
    {
        public RoutePickerViewModel ViewModel { get; }
        public RoutePickerWindow(RoutePickerViewModel vm)
        {
            InitializeComponent();
            ViewModel = vm;
            DataContext = vm;
        }
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.LocationItems.Any(l => l.IsSelected))
            {
                MessageBox.Show("Please select at least one location.",
                                "No Location Selected",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
