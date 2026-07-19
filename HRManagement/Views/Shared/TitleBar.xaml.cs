using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HRManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace HRManagement.Views.Shared;

public partial class TitleBar : UserControl
{
    public TitleBar()
    {
        InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this))
            return;

        DataContext = App.Services.GetRequiredService<TitleBarViewModel>();
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var window = Window.GetWindow(this);

        if (window == null)
            return;

        // Double click to maximize/restore
        if (e.ClickCount == 2)
        {
            if (window.WindowState == WindowState.Maximized)
                window.WindowState = WindowState.Normal;
            else
                window.WindowState = WindowState.Maximized;

            return;
        }

        // Drag window
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            window.DragMove();
        }
    }
}