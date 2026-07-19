using HRManagement.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Controls;

namespace HRManagement.Views.Shared;

public partial class Sidebar : UserControl
{
    public Sidebar()
    {
        InitializeComponent();

        if (DesignerProperties.GetIsInDesignMode(this))
            return;

        DataContext = App.Services.GetRequiredService<SidebarViewModel>();
    }
}