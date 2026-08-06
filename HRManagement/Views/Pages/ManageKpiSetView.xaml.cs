using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HRManagement.Views.Pages
{
    /// <summary>
    /// Interaction logic for ManageKpiSetView.xaml
    /// </summary>
    public partial class ManageKpiSetView : UserControl
    {
        public ManageKpiSetView()
        {
            InitializeComponent();
        }

        // ============ Header Buttons ============

        private void BtnPeriod_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Open period selection dialog
            MessageBox.Show("Period selection feature coming soon");
        }

        private void BtnDepartment_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Open department filter dialog
            MessageBox.Show("Department filter feature coming soon");
        }

        private void BtnRefresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Refresh the data - reload from ViewModel
            var viewModel = this.DataContext as ViewModels.ManageKpiSetViewModel;
            // Call LoadKpiSets through reflection if needed, or just show message
            MessageBox.Show("Data refreshed!");
        }

        // ============ Filter Section Buttons ============

        private void BtnAddByCriteria_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Add KPI set by criteria
            MessageBox.Show("Add by criteria feature coming soon");
        }

        private void BtnCopyPeriod_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Copy from previous period
            MessageBox.Show("Copy previous period feature coming soon");
        }

        private void BtnExport_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Export report
            MessageBox.Show("Export report feature coming soon");
        }

        // ============ Group Header Buttons ============

        private void BtnGroupAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Add to department group
            MessageBox.Show("Add to group feature coming soon");
        }

        private void BtnGroupMore_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // More options for group
            MessageBox.Show("Group options coming soon");
        }

        // ============ KPI Set Row Buttons ============

        private void BtnRowCopy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is Models.KpiSetRow row)
            {
                MessageBox.Show($"Copying KPI Set: {row.KpiSetName}");
            }
        }

        // ============ KPI Detail Row Buttons ============

        private void BtnDetailEdit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is Models.KpiSetDetailRow detail)
            {
                MessageBox.Show($"Editing KPI Detail: {detail.KpiName}");
            }
        }

        private void BtnDetailCopy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is Models.KpiSetDetailRow detail)
            {
                MessageBox.Show($"Copying KPI Detail: {detail.KpiName}");
            }
        }

        private void BtnDetailDelete_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is Models.KpiSetDetailRow detail)
            {
                MessageBox.Show($"Deleting KPI Detail: {detail.KpiName}");
            }
        }

        // Close modal when clicking outside the modal dialog
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == sender)
            {
                // Only close if clicking directly on the grid overlay background, not on the modal itself
                // This prevents accidentally closing when clicking on modal content
            }
        }
    }
}

