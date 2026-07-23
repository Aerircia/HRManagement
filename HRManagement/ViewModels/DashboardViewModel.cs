using HRManagement.Services;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class DashboardViewModel : PageViewModel
{
    private readonly SessionManager _sessionManager;

    public DashboardViewModel(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public override string Title => "Dashboard";

    public string UserName =>
        "Welcome back, " + _sessionManager.CurrentUser?.Employee.FullName ?? "";
}