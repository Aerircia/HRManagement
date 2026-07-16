using HRManagement.Utilities;
using HRManagement.Views.Windows;
using System.Windows;
using System.Windows.Input;

namespace HRManagement.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private string _username = "";
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(_ => Login());
    }

    private void Login()
    {
        MainWindow window = new MainWindow();
        window.Show();

        foreach (Window w in Application.Current.Windows)
        {
            if (w is LoginWindow)
            {
                w.Close();
                break;
            }
        }
    }
}