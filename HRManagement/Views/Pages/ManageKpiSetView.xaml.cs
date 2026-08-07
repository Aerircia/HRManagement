using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HRManagement.Views.Pages
{
    /// <summary>
    /// Interaction logic for ManageKpiSetView.xaml.
    /// All KPI Set CRUD actions (Add/Edit/Delete/Refresh/Search/Department
    /// filter) are wired through ManageKpiSetViewModel commands and
    /// bindings - see IManageKpiSetService for the backing service calls.
    /// The only code-behind logic left is the modal-scrim
    /// click-outside-to-close convenience for the Add/Edit and Delete
    /// overlays (ModalScrimStyle), matching the pattern used by
    /// ManageAnnouncementsView/ManageProfilesView.
    /// </summary>
    public partial class ManageKpiSetView : UserControl
    {
        public ManageKpiSetView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Closes the Add/Edit form overlay when the user clicks the dimmed
        /// scrim background, but not when the click originates from inside
        /// the modal card itself (e.contentSource check).
        /// </summary>
        private void FormScrim_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == sender && DataContext is ViewModels.ManageKpiSetViewModel vm)
            {
                vm.CancelCommand.Execute(null);
            }
        }

        /// <summary>
        /// Closes the Delete confirmation overlay when the user clicks the
        /// dimmed scrim background, but not when the click originates from
        /// inside the confirmation card itself.
        /// </summary>
        private void DeleteScrim_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == sender && DataContext is ViewModels.ManageKpiSetViewModel vm)
            {
                vm.CancelDeleteCommand.Execute(null);
            }
        }
    }
}
