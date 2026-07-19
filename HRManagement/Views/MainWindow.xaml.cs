using HRManagement.ViewModels;
using System.Windows;

namespace HRManagement.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}