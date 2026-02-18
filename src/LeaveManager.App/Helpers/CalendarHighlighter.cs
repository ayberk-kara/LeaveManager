using LeaveManager.App;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace LeaveManager.Helpers
{
    public static class CalendarHighlighter
    {
        private static readonly Brush AnnualBrush = Brushes.LightGreen;   
        private static readonly Brush SickBrush = Brushes.LightBlue;      
        private static readonly Brush DefaultBrush = Brushes.White;

   
        public static void HighlightLeaves(
            Calendar[] calendars,
            ObservableCollection<LeaveItem> leaves)
        {
            foreach (var cal in calendars)
            {
                ResetCalendarHighlights(cal);

                cal.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var dayButton in FindCalendarDayButtons(cal))
                    {
                        if (dayButton.DataContext is DateTime dt)
                        {
                            foreach (var leave in leaves)
                            {
                                if (dt >= leave.StartDate.Date && dt <= leave.EndDate.Date)
                                {
                                    
                                    Console.WriteLine($"Date: {dt.ToShortDateString()}, Leave Type: '{leave.Type}'");


                                    var type = leave.Type?.Trim().ToLowerInvariant() ?? string.Empty;

                                    if (type.Contains("yıllık"))
                                        dayButton.Background = AnnualBrush;
                                    else if (type.Contains("rapor"))
                                        dayButton.Background = SickBrush;
                                    else
                                        dayButton.Background = DefaultBrush;
                                }
                            }
                        }
                    }
                });
            }
        }

        
        private static void ResetCalendarHighlights(Calendar cal)
        {
            cal.Dispatcher.InvokeAsync(() =>
            {
                foreach (var dayButton in FindCalendarDayButtons(cal))
                {
                    dayButton.Background = DefaultBrush;
                }
            });
        }

        
        private static IEnumerable<CalendarDayButton> FindCalendarDayButtons(Calendar cal)
        {
            if (cal.Template.FindName("PART_CalendarItem", cal) is CalendarItem ci)
                return ci.FindVisualChildren<CalendarDayButton>();

            return Array.Empty<CalendarDayButton>();
        }

        
        private static IEnumerable<T> FindVisualChildren<T>(this System.Windows.DependencyObject depObj) where T : System.Windows.DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;

                foreach (var childOfChild in child.FindVisualChildren<T>())
                    yield return childOfChild;
            }
        }
    }
}