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
using System.Windows.Forms.Design;

namespace HRManagement;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        Services = services.BuildServiceProvider();

        // Apply the persisted theme (Brushes.xaml vs DarkBrushes.xaml) before
        // any window is shown. This previously never ran anywhere, so the app
        // always launched in light mode - including the login window -
        // regardless of what the user last chose in Settings, until they
        // happened to toggle it again after logging in.
        Services.GetRequiredService<ISettingService>().Initialize();

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
        services.AddTransient<IPaidTimeOffService, PaidTimeOffService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IManageAttendancesService, ManageAttendancesService>();
        services.AddSingleton<IManageAnnouncementsService, ManageAnnouncementsService>();
        services.AddSingleton<IManageKpiSetService, ManageKpiSetService>();
        services.AddSingleton<IKpiService, KpiService>();


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
        services.AddSingleton<IDashboardRepository, DashboardRepository>();
        services.AddSingleton<IPositionRepository, PositionRepository>();
        services.AddSingleton<IKpiRepository, KpiRepository>();
        services.AddSingleton<IKpiSetRepository, KpiSetRepository>();
        services.AddSingleton<IEmployeeKpiAssignmentRepository, EmployeeKpiAssignmentRepository>();


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
        services.AddTransient<KpiAssignmentViewModel>();
        services.AddTransient<PersonalKpiViewModel>();
        services.AddTransient<ManageKpiSetViewModel>();

        // Windows
        services.AddTransient<MainWindow>();
        services.AddTransient<LoginWindow>();
    }
}
