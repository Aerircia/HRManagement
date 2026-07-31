using HRManagement.Data;
using HRManagement.Repositories;
using HRManagement.Repositories.Interfaces;
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
        // Data
        services.AddSingleton<DatabaseContext>();

        // Services
        services.AddSingleton<SessionManager>();

        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IAuthorizationService, AuthorizationService>();
        services.AddSingleton<IRequestService, RequestService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IAttendanceService, AttendanceService>();
        services.AddSingleton<ISalaryCalculator, SalaryCalculator>();
        services.AddSingleton<ISettingService, SettingService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IManageSalariesService, ManageSalariesService>();
        services.AddSingleton<IEmployeeEvaluationService, EmployeeEvaluationService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IContractService, ContractService>();
        services.AddSingleton<IManageProfilesService, ManageProfilesService>();
        services.AddTransient<IPaidTimeOffService,PaidTimeOffService>();


        // Repositories
        services.AddSingleton<IManageSalariesRepository, ManageSalariesRepository>();
        services.AddSingleton<ISalaryRepository, SalaryRepository>();
        services.AddSingleton<IAccountRepository, AccountRepository>();
        services.AddSingleton<IEmployeeRepository, EmployeeRepository>();
        services.AddSingleton<IRoleRepository, RoleRepository>();
        services.AddSingleton<IDepartmentRepository, DepartmentRepository>();
        services.AddSingleton<IContractRepository, ContractRepository>();
        services.AddSingleton<IAttendanceRepository, AttendanceRepository>();
        services.AddSingleton<IEmployeeEvaluationRepository, EmployeeEvaluationRepository>();
        services.AddSingleton<IPayrollRepository, PayrollRepository>();
        services.AddSingleton<IRequestFormRepository, RequestFormRepository>();
        services.AddSingleton<IAnnouncementRepository, AnnouncementRepository>();
        services.AddSingleton<ISystemLogRepository, SystemLogRepository>();


        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SidebarViewModel>();
        services.AddSingleton<TopBarViewModel>();

        services.AddTransient<TitleBarViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<AttendanceViewModel>();
        services.AddTransient<ContractViewModel>();
        services.AddTransient<ManageContractsViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<EmployeeEvaluationViewModel>();
        services.AddTransient<LogsViewModel>();
        services.AddTransient<ManageAttendancesViewModel>();
        services.AddTransient<ManageProfilesViewModel>();
        services.AddTransient<ManageRequestsViewModel>();
        services.AddTransient<ManageSalariesViewModel>();
        services.AddTransient<ManageAnnouncementsViewModel>();
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<RequestsViewModel>();
        services.AddTransient<SalaryViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Windows
        services.AddTransient<MainWindow>();
        services.AddTransient<LoginWindow>();
    }
}
