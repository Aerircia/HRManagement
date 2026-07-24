using HRManagement.Models;

namespace HRManagement.Services.Interfaces;

public interface ISettingService
{
    AppSettings CurrentSettings { get; }

    string CurrentTheme { get; }

    bool IsLargeText { get; }
    void Initialize();

    void SetLightTheme();
    void SetDarkTheme();
    void ToggleTheme();

    void SetLargeText(bool isLarge);
    void ToggleLargeText();

    void Save();
}
