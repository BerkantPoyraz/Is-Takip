using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;

namespace WorkTrackingWpf
{
    public partial class ReportsLog : Window
    {
        private readonly AppDbContext _context;

        public ReportsLog(AppDbContext context)
        {
            _context = context;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = DateTime.Now.Year - 10; i <= DateTime.Now.Year + 10; i++)
            {
                YearComboBox.Items.Add(i);
            }
            YearComboBox.SelectedItem = DateTime.Now.Year;

            for (int i = 1; i <= 12; i++)
            {
                MonthComboBox.Items.Add(i);
            }
            MonthComboBox.SelectedItem = DateTime.Now.Month;
        }

        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (YearComboBox.SelectedItem != null && MonthComboBox.SelectedItem != null)
            {
                int selectedYear = (int)YearComboBox.SelectedItem;
                int selectedMonth = (int)MonthComboBox.SelectedItem;

                await LoadUserReportsAsync(selectedYear, selectedMonth);
            }
            else
            {
                MessageBox.Show("Lütfen yıl ve ay seçiniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task LoadUserReportsAsync(int year, int month)
        {
            try
            {
                if (App.CurrentUser == null)
                {
                    MessageBox.Show("Giriş yapılmadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var daysInMonth = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                                             .Select(day => new DateTime(year, month, day))
                                             .ToList();

                var userReports = await _context.UserReports
                     .Where(r => r.UserId == App.CurrentUser.UserId && r.ReportDate.Year == year && r.ReportDate.Month == month)
                     .ToListAsync();

                double totalMissingTime = 0;
                double totalOvertime = 0;

                var groupedReports = userReports
                     .Select(report =>
                     {
                         double missingTime = 0;
                         double overtime = 0;

                         var workStart = report.WorkStart ?? DateTime.MinValue;
                         var workEnd = report.WorkEnd ?? DateTime.MinValue;
                         var lunchStart = report.LunchStart ?? DateTime.MinValue;
                         var lunchEnd = report.LunchEnd ?? DateTime.MinValue;

                         double workDurationInMinutes = 0;
                         if (report.WorkStart.HasValue && report.WorkEnd.HasValue)
                         {
                             workDurationInMinutes = (workEnd - workStart).TotalMinutes;
                             if (report.LunchStart.HasValue && report.LunchEnd.HasValue)
                             {
                                 workDurationInMinutes -= (lunchEnd - lunchStart).TotalMinutes;
                             }
                         }

                         var idleTime = report.IdleTime ?? TimeSpan.Zero;
                         workDurationInMinutes -= idleTime.TotalMinutes;

                         bool isWeekend = report.ReportDate.DayOfWeek == DayOfWeek.Saturday || report.ReportDate.DayOfWeek == DayOfWeek.Sunday;

                         const double expectedDailyWorkMinutes = 9 * 60;

                         if (isWeekend)
                         {
                             overtime += workDurationInMinutes;
                         }
                         else
                         {
                             if (workDurationInMinutes < expectedDailyWorkMinutes)
                             {
                                 missingTime = expectedDailyWorkMinutes - workDurationInMinutes;
                             }
                             else
                             {
                                 overtime = workDurationInMinutes - expectedDailyWorkMinutes;
                             }
                         }

                         totalMissingTime += missingTime;
                         totalOvertime += overtime;

                         return new
                         {
                             WorkDate = report.ReportDate.ToString("dd.MM.yyyy dddd", new CultureInfo("tr-TR")),
                             WorkStartTime = report.WorkStart?.ToString("HH:mm") ?? "00:00",
                             EndTime = report.WorkEnd?.ToString("HH:mm") ?? "00:00",
                             LunchStartTime = report.LunchStart?.ToString("HH:mm") ?? "00:00",
                             LunchEndTime = report.LunchEnd?.ToString("HH:mm") ?? "00:00",
                             MissingTime = missingTime > 0 ? TimeSpan.FromMinutes(missingTime).ToString(@"hh\:mm") : "00:00",
                             Overtime = overtime > 0 ? TimeSpan.FromMinutes(overtime).ToString(@"hh\:mm") : "00:00"
                         };
                     })
                     .ToList();

                var loggedDates = userReports.Select(r => r.ReportDate).Distinct().ToList();
                var missingDates = daysInMonth.Except(loggedDates).ToList();

                foreach (var missingDate in missingDates)
                {
                    bool isWeekendMissing = missingDate.DayOfWeek == DayOfWeek.Saturday || missingDate.DayOfWeek == DayOfWeek.Sunday;

                    if (isWeekendMissing)
                    {
                        groupedReports.Add(new
                        {
                            WorkDate = missingDate.ToString("dd.MM.yyyy dddd", new CultureInfo("tr-TR")),
                            WorkStartTime = "00:00",
                            EndTime = "00:00",
                            LunchStartTime = "00:00",
                            LunchEndTime = "00:00",
                            MissingTime = "00:00",
                            Overtime = "00:00"
                        });
                    }
                    else
                    {
                        groupedReports.Add(new
                        {
                            WorkDate = missingDate.ToString("dd.MM.yyyy dddd", new CultureInfo("tr-TR")),
                            WorkStartTime = "00:00",
                            EndTime = "00:00",
                            LunchStartTime = "00:00",
                            LunchEndTime = "00:00",
                            MissingTime = TimeSpan.FromMinutes(540).ToString(@"hh\:mm"), // 9 saat eksik
                            Overtime = "00:00"
                        });

                        totalMissingTime += 540;
                    }
                }

                groupedReports = groupedReports.OrderBy(r => DateTime.ParseExact(r.WorkDate.Split(' ')[0], "dd.MM.yyyy", null)).ToList();
                ReportsDataGrid.ItemsSource = groupedReports;

                TotalMissingTime.Text = FormatToTotalHours(totalMissingTime);
                TotalOvertime.Text = FormatToTotalHours(totalOvertime);

                double totalNetTime = totalOvertime - totalMissingTime;
                TotalNetWorkDuration.Text = FormatToTotalHours(Math.Abs(totalNetTime));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string FormatToTotalHours(double totalMinutes)
        {
            int hours = (int)(totalMinutes / 60);
            int minutes = (int)(totalMinutes % 60);
            return $"{hours:D2}:{minutes:D2}";
        }

        private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DataGrid_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}