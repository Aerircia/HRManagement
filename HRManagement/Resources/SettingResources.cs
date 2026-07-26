namespace HRManagement.Resources;

public static class SettingResources
{
    // ===== Theme identifiers =====
    public const string ThemeLight = "Light";
    public const string ThemeDark = "Dark";

    // ===== Resource dictionary locations (pack URIs, relative to assembly root) =====
    public const string LightBrushesPath = "Resources/Brushes.xaml";
    public const string DarkBrushesPath = "Resources/DarkBrushes.xaml";

    // ===== Global font size =====
    public const string FontSizeResourceKey = "AppFontSize";
    public const double NormalFontSize = 14d;
    public const double LargeFontSize = 20d;

    // ===== Local persistence =====
    public const string SettingsFolderName = "HRManagement";
    public const string SettingsFileName = "settings.json";
}
