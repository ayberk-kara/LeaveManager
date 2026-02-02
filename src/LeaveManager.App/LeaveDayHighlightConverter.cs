using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace LeaveManager.App
{
    public sealed class LeaveDayHighlightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            DateTime? day = TryGetDate(values[0]);
            if (day == null)
                return false;

            HashSet<DateTime>? set = TryGetDateSet(values[1]);
            if (set == null)
                return false;

            return set.Contains(day.Value.Date);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static DateTime? TryGetDate(object value)
        {
            if (value == null)
                return null;

            // Most common
            if (value is DateTime dt)
                return dt.Date;

            // Sometimes nullable DateTime boxed as object
            if (value is DateTime?)
            {
                var tmp = (DateTime?)value;
                if (tmp.HasValue) return tmp.Value.Date;
            }

            // Sometimes a UI element is passed in; check DataContext
            if (value is FrameworkElement fe)
            {
                if (fe.DataContext is DateTime d1) return d1.Date;

                if (fe.DataContext is DateTime?)
                {
                    var tmp2 = (DateTime?)fe.DataContext;
                    if (tmp2.HasValue) return tmp2.Value.Date;
                }
            }

            // Sometimes CalendarDayButton
            if (value is CalendarDayButton btn)
            {
                if (btn.DataContext is DateTime d1) return d1.Date;

                if (btn.DataContext is DateTime?)
                {
                    var tmp3 = (DateTime?)btn.DataContext;
                    if (tmp3.HasValue) return tmp3.Value.Date;
                }
            }

            return null;
        }

        private static HashSet<DateTime>? TryGetDateSet(object value)
        {
            if (value == null)
                return null;

            if (value is HashSet<DateTime> hs)
                return hs;

            if (value is IEnumerable<DateTime> enumerable)
                return new HashSet<DateTime>(enumerable.Select(d => d.Date));

            return null;
        }
    }
}
