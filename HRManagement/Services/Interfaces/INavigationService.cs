using System.ComponentModel;
using HRManagement.Utilities;

namespace HRManagement.Services.Interfaces;

public interface INavigationService : INotifyPropertyChanged
{
    ViewModelBase? CurrentView { get; }

    void Navigate<TViewModel>()
        where TViewModel : ViewModelBase;

    void Navigate(Type viewModelType);
}