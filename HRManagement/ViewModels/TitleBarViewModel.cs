using System.Windows.Input;
using HRManagement.Services.Interfaces;
using HRManagement.Utilities;

namespace HRManagement.ViewModels;

public class TitleBarViewModel : ViewModelBase
{
    private readonly IWindowService _windowService;

    public string Title => "HR Manager";

    public ICommand MinimizeCommand { get; }

    public ICommand MaximizeCommand { get; }

    public ICommand CloseCommand { get; }

    public TitleBarViewModel(IWindowService windowService)
    {
        _windowService = windowService;

        MinimizeCommand = new RelayCommand(_ => _windowService.Minimize());

        MaximizeCommand = new RelayCommand(_ => _windowService.MaximizeRestore());

        CloseCommand = new RelayCommand(_ => _windowService.CloseCurrentWindow());
    }
}