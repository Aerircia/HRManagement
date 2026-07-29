using HRManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HRManagement.Views.Pages
{
    /// <summary>
    /// Interaction logic for EmployeeEvaluationView.xaml
    /// </summary>
    public partial class EmployeeEvaluationView : UserControl
    {
        private bool _isViewLoaded;

        public EmployeeEvaluationView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isViewLoaded)
                return;

            _isViewLoaded = true;

            if (DataContext is not EmployeeEvaluationViewModel viewModel)
                return;

            if (viewModel.RefreshCommand.CanExecute(null))
            {
                viewModel.RefreshCommand.Execute(null);
            }
        }

        private void DepartmentComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            
            if (!_isViewLoaded)
                return;

            if (DataContext is not EmployeeEvaluationViewModel viewModel)
                return;

            if (viewModel.SearchCommand.CanExecute(null))
            {
                viewModel.SearchCommand.Execute(null);
            }
        }
    }
}

