using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace HRManagement.Utilities;

/// <summary>
/// Converts a relative avatar path (e.g. "Assets\Avatars\Admin.png", as
/// stored on Employee.Avatar) into a BitmapImage loaded via pack URI, so it
/// can be bound directly to Image.Source / ImageBrush.ImageSource.
///
/// Handles:
///   - null/empty -> default avatar
///   - backslash-separated paths (as stored in the DB) -> normalized to '/'
///   - a leading '/' (already-rooted paths like some ViewModel fallbacks use)
///   - a path that fails to resolve to an existing pack resource -> default avatar,
///     instead of throwing/leaving a broken Image at runtime
///
/// Default avatar lives at Assets/Avatars/DefaultAvatar.png. If a
/// ViewModel-level fallback string points elsewhere (e.g.
/// "/Resources/Images/DefaultAvatar.png"), that string is still normalized
/// and attempted first; this converter only substitutes its own default
/// when the given path can't be resolved at all.
/// </summary>
public class AvatarPathConverter : IValueConverter
{
    private const string DefaultAvatarPath = "Assets/Avatars/DefaultAvatar.png";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var path = value as string;

        var resolved = TryLoad(path);
        if (resolved != null)
            return resolved;

        // Given path missing/invalid - fall back to the app default avatar.
        var fallback = TryLoad(DefaultAvatarPath);
        if (fallback != null)
            return fallback;

        // Even the default avatar couldn't be resolved (e.g. not present in
        // the build output yet) - return null rather than throwing, so the
        // binding's own FallbackValue (if set) or a blank image is used
        // instead of crashing the UI thread.
        return null!;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static BitmapImage? TryLoad(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var normalized = path.Replace('\\', '/').TrimStart('/');

        try
        {
            var uri = new Uri($"pack://application:,,,/{normalized}", UriKind.Absolute);

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = uri;
            image.EndInit();
            image.Freeze();

            return image;
        }
        catch
        {
            // Missing resource, bad path, etc. - let the caller fall back.
            return null;
        }
    }
}
