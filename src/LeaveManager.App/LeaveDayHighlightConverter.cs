using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace LeaveManager.App
{
    public sealed class LeaveDayHighlightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            if (values[0] is not DateTime day)
                return false;

            if (values[1] is not HashSet<DateTime> set)
                return false;

            return set.Contains(day.Date);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
