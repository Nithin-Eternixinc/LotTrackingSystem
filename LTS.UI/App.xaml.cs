
using LTS.Application.Data;
using LTS.Application.Repositories;
using LTS.Application.Services;
using LTS.UI.ViewModels;
using LTS.UI.Views;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using System.Data;
using System.Windows;

namespace LTS.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static AppDbContext Database { get; private set; } = null!; //creates a global place to access the db
        public static LogService LogService { get; private set; } = null!;
        public static AuthService AuthService { get; private set; } = null!;
        public static SupplierService SupplierService { get; private set; } = null!;
        public static WaferService WaferService { get; private set; } = null!;
        public static CarrierService CarrierService { get; private set; } = null!;
        public static LotService LotService { get; private set; } = null!;
        public static ProcessLocationService ProcessLocationService { get; set; } = null!;


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                //set up sqlite
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite("Data Source=lts_database.db") // EF Core configuration and tells it to use a SQLite file named lts_database.db.
                    .Options;

                Database = new AppDbContext(options); //creates database context

                Database.Database.EnsureCreated(); //creates the database file and tables if they do not already exist

                // 2. build repositories 
                var logRepo = new LogRepository(Database);
                var supplierRepo = new SupplierRepository(Database);
                var waferRepo = new WaferRepository(Database);
                var carrierRepo = new CarrierRepository(Database);
                var lotRepo = new LotRepository(Database);
                var locationRepo = new ProcessLocationRepository(Database);
                var routeRepo = new LotRouteRepository(Database);

                // 3. build services 
                LogService = new LogService(logRepo);
                AuthService = new AuthService(Database, LogService);
                SupplierService = new SupplierService(supplierRepo, LogService);
                WaferService = new WaferService(waferRepo, LogService);
                CarrierService = new CarrierService(carrierRepo, LogService);
                LotService = new LotService(lotRepo, waferRepo, routeRepo, carrierRepo, LogService);
                ProcessLocationService = new ProcessLocationService(locationRepo, lotRepo, routeRepo,carrierRepo, LogService);

                var loginVM = new LoginViewModel(AuthService);
                var loginWindow = new LoginView(loginVM);
                loginWindow.Show();
                
            }

            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error: {ex.Message}\n\nInner: {ex.InnerException?.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }




    }

}
