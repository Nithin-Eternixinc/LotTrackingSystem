using LTS.Application.Services;
using System.ComponentModel.DataAnnotations;

namespace LTS.UI.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService;

    private string _username = "";
    private string _password = "";
    private string _errorMessage = "";
    private bool _isLoading;

    public string Username
    {
        get => _username;
        set { SetProperty(ref _username, value); LoginCommand.RaiseCanExecuteChanged(); }
    }
    public string Password
    {
        get => _password;
        set { SetProperty(ref _password, value); LoginCommand.RaiseCanExecuteChanged(); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public AsyncRelayCommand LoginCommand { get; }

    // event — tells the View to open MainWindow
    public event Action? LoginSucceeded;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;

        LoginCommand = new AsyncRelayCommand(
            execute: LoginAsync,
            canExecute: () => !string.IsNullOrWhiteSpace(Username) &&
                              !string.IsNullOrWhiteSpace(Password) &&
                              !IsLoading
        );
    }

    private async Task LoginAsync()
    {
        IsLoading = true;
        ErrorMessage = "";

        var user = await _authService.LoginAsync(Username, Password);

        IsLoading = false;

        if (user == null)
        {
            ErrorMessage = "Invalid username or password.";
            return;
        }

        // notify view to navigate ,fire event
        LoginSucceeded?.Invoke();
    }
}