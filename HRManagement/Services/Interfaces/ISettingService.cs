using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

public interface ISettingService
{
    AppSettings CurrentSettings { get; }

    string CurrentTheme { get; }

    void Initialize();

    void SetLightTheme();
    void SetDarkTheme();
    void ToggleTheme();

    void Save();

    /// <summary>
    /// Changes the password for the given account, verifying the current
    /// password first. Returns false if the current password does not
    /// match. Throws <see cref="ArgumentException"/> if the new password
    /// does not meet complexity requirements (8+ characters, at least one
    /// uppercase letter, at least one OWASP special character) - this is
    /// the service-layer guard and holds even if the caller/UI failed to
    /// validate first.
    /// </summary>
    bool ChangePassword(int accountId, string currentPassword, string newPassword);

    /// <summary>
    /// Raised after the merged brush dictionary (Brushes.xaml/DarkBrushes.xaml)
    /// has actually been swapped. DynamicResource bindings in XAML pick up the
    /// new palette automatically, but anything that resolves a brush/color
    /// itself in code - value converters that call Application.Current.FindResource
    /// once per binding update (not once per theme change), or non-WPF consumers
    /// like LiveCharts' SkiaSharp paints - needs this to know when to re-resolve
    /// or rebuild, since neither of those cases re-fires on its own.
    /// </summary>
    event EventHandler? ThemeChanged;
}
