using System.Globalization;
using System.Windows.Data;

namespace HRManagement.Utilities;

// Returns true when the bound int is strictly greater than the int parsed
// from ConverterParameter. Used by the Assign KPI wizard's step-header
// circles (KpiAssignmentView.xaml) to mark a step "completed" (WizardStep
// > that step's number) as distinct from "active" (WizardStep == N, via
// the existing IsStep1..IsStep4 properties) - no existing converter in
// Converters.xaml does numeric comparison, so this is a small addition
// rather than overloading an unrelated one.
public class StepGreaterThanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int current)
            return false;

        if (parameter == null || !int.TryParse(parameter.ToString(), out var threshold))
            return false;

        return current > threshold;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
