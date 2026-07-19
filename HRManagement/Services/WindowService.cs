using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using HRManagement.Services.Interfaces;
using HRManagement.Views;

namespace HRManagement.Services;

public class WindowService : IWindowService
{
    public void ShowMainWindow()
    {
        var mainWindow = App.Services.GetRequiredService<MainWindow>();

        Application.Current.MainWindow = mainWindow;

        mainWindow.Show();

        Close<LoginWindow>();
    }

    public void ShowLoginWindow()
    {
        var loginWindow = App.Services.GetRequiredService<LoginWindow>();

        Application.Current.MainWindow = loginWindow;

        loginWindow.Show();

        Close<MainWindow>();
    }

    private static void Close<T>() where T : Window
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window is T)
            {
                window.Close();
                break;
            }
        }
    }
    public void Minimize()
    {
        Application.Current.MainWindow!.WindowState = WindowState.Minimized;
    }

    public void MaximizeRestore()
    {
        var window = Application.Current.MainWindow!;

        window.WindowState =
            window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }

    public void CloseCurrentWindow()
    {
        Application.Current.MainWindow?.Close();
    }
}