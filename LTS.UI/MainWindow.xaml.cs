using LTS.UI.ViewModels;
using System.Windows;

namespace LTS.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainViewModel(
            App.SupplierService,
            App.WaferService,
            App.LotService,
            App.ProcessLocationService,
            App.AuthService,           
            App.LogService,
            App.CarrierService
        );

        DataContext = vm; //vm datacontext object ,UI connected to ViewModel
    }
}