using LTS.UI.ViewModels;
using System.Windows;

namespace LTS.UI.Views;

public partial class LoginView : Window
{
    private readonly LoginViewModel _vm; //Stores ViewModel object inside this window

    public LoginView(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        // wire password box — PasswordBox doesn't support binding
        PasswordBox.PasswordChanged += (s, e) => // s sender which obj raised event ,e contains extra infor about the event.
            _vm.Password = PasswordBox.Password;

        // when login succeeds open MainWindow
        _vm.LoginSucceeded += () =>
        {
           
            var mainWindow = new MainWindow();
            mainWindow.Show();

            this.Close();
        };
    }

  
}