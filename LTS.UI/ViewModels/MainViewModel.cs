using LTS.Application.Repositories;
using LTS.Application.Services;
using LTS.UI.ViewModels;
using LTS.UI.Views;
using System;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;

namespace LTS.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private readonly LogService _logService;
    private readonly SupplierService _supplierService;
    private readonly WaferService _waferService;
    private readonly CarrierService _carrierService;
    private readonly LotService _lotService;
    private readonly ProcessLocationService _locationService;


    private object _currentView = null!; //declared as object, it can hold any view instance

    private DashboardView? _dashboardView; //Stores dashboard page in memory.


    public object CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }
    //logged user name
    public string LoggedInUser
        => SessionManager.CurrentUser?.UserName ?? "";

    public bool IsEngineer
        => SessionManager.IsEngineer;

    public ICommand NavToDashboardCommand { get; } //Commands connected to buttons.
    public ICommand NavToSupplierCommand { get; }
    public ICommand NavToWaferCommand { get; }
    public ICommand NavToCarrierCommand { get; }
    public ICommand NavToLotCommand { get; }
    public ICommand NavToLogCommand { get; }
    public ICommand LogoutCommand { get; }


    public MainViewModel(
        SupplierService supplierService,
        WaferService waferService,
        LotService lotService,
        ProcessLocationService processLocationService,
        AuthService AuthService,
        LogService logService,
        CarrierService carrierService
        )
    {
        _supplierService = supplierService;
        _waferService = waferService;
        _lotService = lotService;
        _locationService = processLocationService;
        _authService = AuthService;
        _logService = logService;
        _carrierService = carrierService;
        LogoutCommand = new RelayCommand(async _ => await Logout());




        NavToDashboardCommand = new RelayCommand(() =>
        {
            if (_dashboardView == null)
                _dashboardView = new DashboardView(
                    new DashboardViewModel(
                        App.LotService,
                        App.WaferService,
                        App.ProcessLocationService,
                        App.LogService
                    ));

            var vm = _dashboardView.DataContext as DashboardViewModel;

            (_dashboardView.DataContext as DashboardViewModel)?.RefreshCommand.Execute(null);
            CurrentView = _dashboardView;
        });
        
        NavToSupplierCommand = new RelayCommand(() =>
        CurrentView = new SupplierView(new SupplierViewModel(App.SupplierService)));

        NavToWaferCommand = new RelayCommand(() =>
        CurrentView = new WaferView(new WaferViewModel(_waferService, _supplierService)));

        NavToCarrierCommand = new RelayCommand(() =>
        CurrentView = new CarrierView(new CarrierViewModel(_carrierService)));

        NavToLotCommand = new RelayCommand(() =>
        CurrentView = new LotView(new LotViewModel(_lotService, _waferService, _carrierService, _supplierService)));

        NavToLogCommand = new RelayCommand(() =>
        CurrentView = new LogView(new LogViewModel(_logService)));

        NavToDashboardCommand.Execute(null); //defaul dashboard
       
    }

    public async Task Logout()
    {
        await _authService.LogoutAsync();

        var loginVM = new LoginViewModel(_authService);
        var loginWindow = new LoginView(loginVM);
        loginWindow.Show();

        System.Windows.Application.Current.MainWindow = loginWindow;


        foreach (Window window in System.Windows.Application.Current.Windows)
        {
            if (window is MainWindow)
            {
                window.Close();
                break;
            }
        }

    }

}
