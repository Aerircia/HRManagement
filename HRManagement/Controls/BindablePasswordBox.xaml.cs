using System.Windows;
using System.Windows.Controls;
using FontAwesome.Sharp;

namespace HRManagement.Controls;

public partial class BindablePasswordBox : UserControl
{
    private bool _isUpdating;
    private bool _showPassword;

    public BindablePasswordBox()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty PasswordProperty =
        DependencyProperty.Register(
            nameof(Password),
            typeof(string),
            typeof(BindablePasswordBox),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnPasswordPropertyChanged));

    public string Password
    {
        get => (string)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    private static void OnPasswordPropertyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (BindablePasswordBox)d;

        if (control._isUpdating)
            return;

        var value = e.NewValue?.ToString() ?? string.Empty;

        control.PasswordBox.Password = value;
        control.TextBox.Text = value;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating)
            return;

        _isUpdating = true;

        Password = PasswordBox.Password;
        TextBox.Text = PasswordBox.Password;

        _isUpdating = false;
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating)
            return;

        _isUpdating = true;

        Password = TextBox.Text;
        PasswordBox.Password = TextBox.Text;

        _isUpdating = false;
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _showPassword = !_showPassword;

        if (_showPassword)
        {
            TextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;

            TextBox.Text = PasswordBox.Password;

            EyeIcon.Icon = IconChar.EyeSlash;

            TextBox.Focus();
            TextBox.CaretIndex = TextBox.Text.Length;
        }
        else
        {
            PasswordBox.Visibility = Visibility.Visible;
            TextBox.Visibility = Visibility.Collapsed;

            PasswordBox.Password = TextBox.Text;

            EyeIcon.Icon = IconChar.Eye;

            PasswordBox.Focus();
        }
    }
}