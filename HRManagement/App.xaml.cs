using HRManagement.Services;
using HRManagement.Services.Interfaces;
using HRManagement.ViewModels;
using HRManagement.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace HRManagement;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        Services = services.BuildServiceProvider();

        var loginWindow = Services.GetRequiredService<LoginWindow>();

        loginWindow.Show();

        base.OnStartup(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IWindowService, WindowService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SidebarViewModel>();
        services.AddSingleton<TopBarViewModel>();

        services.AddTransient<TitleBarViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AttendanceViewModel>();
        services.AddTransient<ContractViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<EmployeeEvaluationViewModel>();
        services.AddTransient<LogsViewModel>();
        services.AddTransient<ManageAttendancesViewModel>();
        services.AddTransient<ManageProfilesViewModel>();
        services.AddTransient<ManageRequestsViewModel>();
        services.AddTransient<ManageSalariesViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<RequestsViewModel>();
        services.AddTransient<SalaryViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Windows
        services.AddTransient<MainWindow>();
        services.AddTransient<LoginWindow>();
    }
}