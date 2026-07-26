using System.Windows.Controls;

namespace HRManagement.Controls
{
    /// <summary>
    /// Interaction logic for AttendanceCalendarView.xaml.
    ///
    /// Pure View: holds no logic of its own. Its DataContext is expected to
    /// be an AttendanceViewModel, supplied by whichever parent view hosts it
    /// (AttendanceView binds it implicitly to its own DataContext;
    /// ManageAttendancesView binds it explicitly to ChildAttendanceViewModel).
    /// </summary>
    public partial class AttendanceCalendarView : UserControl
    {
        public AttendanceCalendarView()
        {
            InitializeComponent();
        }
    }
}
