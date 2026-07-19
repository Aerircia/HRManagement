using HRManagement.Utilities;

namespace HRManagement.Models;

public class NavigationItem
{
    public string Title { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public UserRole MinimumRole { get; set; }

    public Type ViewModelType { get; set; } = typeof(ViewModelBase);
}