using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using HRManagement.Models;
using HRManagement.Repositories.Interfaces;
using HRManagement.Resources;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.Services;

public class SettingService : ISettingService
{
    public AppSettings CurrentSettings { get; private set; } = new();

    public string CurrentTheme => CurrentSettings.Theme;

    public event EventHandler? ThemeChanged;

    private readonly string _settingsFilePath;
    private readonly IAccountRepository _accountRepository;

    public SettingService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository
            ?? throw new ArgumentNullException(nameof(accountRepository));

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            SettingResources.SettingsFolderName);

        Directory.CreateDirectory(folder);

        _settingsFilePath = Path.Combine(folder, SettingResources.SettingsFileName);
    }

    public void Initialize()
    {
        Load();
        ApplyTheme(CurrentSettings.Theme);
    }

    public void SetLightTheme()
    {
        CurrentSettings.Theme = SettingResources.ThemeLight;
        ApplyTheme(SettingResources.ThemeLight);
        Save();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetDarkTheme()
    {
        CurrentSettings.Theme = SettingResources.ThemeDark;
        ApplyTheme(SettingResources.ThemeDark);
        Save();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleTheme()
    {
        if (CurrentSettings.Theme == SettingResources.ThemeDark)
            SetLightTheme();
        else
            SetDarkTheme();
    }

    public bool ChangePassword(int accountId, string currentPassword, string newPassword)
    {
        if (!ValidationRules.IsValidPassword(newPassword))
            throw new ArgumentException(ValidationRules.PasswordErrorMessage, nameof(newPassword));

        return _accountRepository.ChangePassword(accountId, currentPassword, newPassword);
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(
                CurrentSettings,
                new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Persisting a preference should never crash the app.
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);

                if (loaded != null)
                    CurrentSettings = loaded;
            }
        }
        catch
        {
            CurrentSettings = new AppSettings();
        }
    }

    /// <summary>
    /// Swaps the merged brush dictionary (Brushes.xaml / DarkBrushes.xaml) so that every
    /// StaticResource/DynamicResource brush lookup in the app picks up the new palette.
    /// Assumes App.xaml merges the Light brushes (Brushes.xaml) by default.
    /// </summary>
    private static void ApplyTheme(string theme)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;

        var themePath = theme == SettingResources.ThemeDark
            ? SettingResources.DarkBrushesPath
            : SettingResources.LightBrushesPath;

        var existing = dictionaries.FirstOrDefault(d =>
            d.Source != null &&
            (d.Source.OriginalString.EndsWith("Brushes.xaml") ||
             d.Source.OriginalString.EndsWith("DarkBrushes.xaml")));

        var newDictionary = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/{themePath}", UriKind.Absolute)
        };

        if (existing != null)
        {
            var index = dictionaries.IndexOf(existing);
            dictionaries[index] = newDictionary;
        }
        else
        {
            dictionaries.Add(newDictionary);
        }
    }
}
