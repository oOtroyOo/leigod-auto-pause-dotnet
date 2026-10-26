namespace LeigodPause.Converters;

internal class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = true;
        if (parameter is string parameterString)
            bool.TryParse(parameterString, out flag);

        var yes = flag ? Visibility.Visible : Visibility.Collapsed;
        var no = flag ? Visibility.Collapsed : Visibility.Visible;

        return (value is null || (value is string s && string.IsNullOrWhiteSpace(s))) ? yes : no;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}