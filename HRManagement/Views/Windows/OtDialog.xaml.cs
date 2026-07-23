using System;
using System.Globalization;
using System.Windows;

namespace HRManagement.Views.Windows;

public partial class OtDialog : Window
{
    public DateTime SelectedDate { get; private set; }
    public TimeSpan? StartTime { get; private set; }
    public TimeSpan? EndTime { get; private set; }

    public OtDialog()
    {
        InitializeComponent();
        DpDate.SelectedDate = DateTime.Now.Date;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        SelectedDate = DpDate.SelectedDate ?? DateTime.Now.Date;
        if (TimeSpan.TryParseExact(TbStart.Text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var s))
            StartTime = s;
        if (TimeSpan.TryParseExact(TbEnd.Text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var en))
            EndTime = en;

        DialogResult = true;
        Close();
    }
}

