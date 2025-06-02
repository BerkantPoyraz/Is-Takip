using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;

namespace WorkTrackingWpf
{
    public partial class AdminUserReport : Window
    {
        private readonly AppDbContext _context;
        public ObservableCollection<Status> StatusOptions { get; set; }


        public AdminUserReport(AppDbContext context)
        {
            _context = context;
            InitializeComponent();
            this.Topmost = true;

            StatusOptions = new ObservableCollection<Status>
            {
                Status.Seçiniz,
                Status.Hata,
                Status.Onay,
                Status.İzinli,
                Status.Raporlu
            };

            // DataContext'i bu pencereye ayarla
            DataContext = this;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
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

            var users = await _context.Users.ToListAsync();
            UserComboBox.ItemsSource = users;
            UserComboBox.DisplayMemberPath = "FullName";
            UserComboBox.SelectedValuePath = "UserId";
        }

        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (YearComboBox.SelectedItem != null && MonthComboBox.SelectedItem != null && UserComboBox.SelectedValue != null)
            {
                int selectedYear = (int)YearComboBox.SelectedItem;
                int selectedMonth = (int)MonthComboBox.SelectedItem;
                int selectedUserId = (int)UserComboBox.SelectedValue;

                await LoadUserReportsAsync(selectedYear, selectedMonth, selectedUserId);
            }
            else
            {
                MessageBox.Show("Lütfen yıl, ay ve kullanıcı seçiniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task LoadUserReportsAsync(int year, int month, int selectedUserId)
        {
            try
            {
                if (selectedUserId <= 0)
                {
                    MessageBox.Show("Geçersiz kullanıcı seçimi.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var today = DateTime.Today;

                var userReports = await _context.UserReports
                    .Where(r => r.UserId == selectedUserId &&
                                r.ReportDate.Year == year &&
                                r.ReportDate.Month == month)
                    .OrderBy(r => r.ReportDate)
                    .ToListAsync();

                double totalMissingTime = 0;
                double totalOvertime = 0;

                var reportList = userReports.Select(report =>
                {
                    double missingTimeMinutes = 0;
                    double overtimeMinutes = 0;

                    var workStart = report.WorkStart ?? DateTime.MinValue;
                    var workEnd = report.WorkEnd ?? DateTime.MinValue;

                    TimeSpan startOfWork = new TimeSpan(8, 0, 0);
                    TimeSpan lunchStart = new TimeSpan(13, 0, 0);
                    TimeSpan lunchEndDefault = new TimeSpan(14, 0, 0);
                    TimeSpan endOfWork = new TimeSpan(18, 0, 0);

                    var lunchDuration = lunchEndDefault - lunchStart;
                    var lunchEnd = report.LunchEnd?.TimeOfDay ?? lunchEndDefault;

                    if (report.ReportDate.DayOfWeek == DayOfWeek.Saturday || report.ReportDate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        if (workStart != DateTime.MinValue && workEnd != DateTime.MinValue)
                        {
                            var totalWorkTime = workEnd - workStart;

                            if (workStart.TimeOfDay < lunchEnd && workEnd.TimeOfDay > lunchStart)
                            {
                                totalWorkTime -= lunchDuration;
                            }

                            overtimeMinutes += totalWorkTime.TotalMinutes;
                        }
                    }
                    else
                    {
                        if (workStart != DateTime.MinValue)
                        {
                            if (workStart.TimeOfDay < startOfWork)
                            {
                                overtimeMinutes += (startOfWork - workStart.TimeOfDay).TotalMinutes;
                            }

                            if (workStart.TimeOfDay > lunchStart && workStart.TimeOfDay < lunchEndDefault)
                            {
                                overtimeMinutes += (lunchEndDefault - workStart.TimeOfDay).TotalMinutes;
                            }
                        }

                        if (lunchEnd > lunchEndDefault)
                        {
                            missingTimeMinutes += (lunchEnd - lunchEndDefault).TotalMinutes;
                        }

                        if (workStart == DateTime.MinValue && workEnd == DateTime.MinValue)
                        {
                            if (report.ReportDate < today) // 📌 **Sadece geçmiş günler için 9 saat eksik mesai ekle**
                            {
                                missingTimeMinutes = 540; // 9 saat
                            }
                            else
                            {
                                missingTimeMinutes = 0; // 📌 **Gelecek günlerde boş bırak**
                            }
                            overtimeMinutes = 0;
                        }
                        else
                        {
                            if (workStart.TimeOfDay > startOfWork && workStart.TimeOfDay < lunchStart)
                            {
                                missingTimeMinutes += (workStart.TimeOfDay - startOfWork).TotalMinutes;
                            }
                            if (workStart.TimeOfDay > lunchEnd && workStart.TimeOfDay < endOfWork)
                            {
                                missingTimeMinutes += (workStart.TimeOfDay - lunchEnd).TotalMinutes;
                            }
                            if (workEnd.TimeOfDay < endOfWork)
                            {
                                missingTimeMinutes += (endOfWork - workEnd.TimeOfDay).TotalMinutes;
                            }

                            if (workEnd.TimeOfDay > endOfWork)
                            {
                                overtimeMinutes += (workEnd.TimeOfDay - endOfWork).TotalMinutes;
                            }

                            if (report.LunchEnd.HasValue && report.LunchEnd.Value.TimeOfDay < lunchEndDefault)
                            {
                                double earlyReturnMinutes = (lunchEndDefault - report.LunchEnd.Value.TimeOfDay).TotalMinutes;
                                overtimeMinutes += earlyReturnMinutes;
                            }
                        }
                    }

                    if (missingTimeMinutes > 540)
                    {
                        missingTimeMinutes = 540;
                    }

                    totalMissingTime += missingTimeMinutes;
                    totalOvertime += overtimeMinutes;

                    return new
                    {
                        report.UserReportsId,
                        report.UserId,
                        WorkDate = report.ReportDate.ToString("dd.MM.yyyy dddd", new CultureInfo("tr-TR")),
                        WorkStartTime = workStart == DateTime.MinValue ? "00:00" : workStart.ToString("HH:mm"),
                        EndTime = workEnd == DateTime.MinValue ? "00:00" : workEnd.ToString("HH:mm"),
                        LunchStartTime = report.LunchStart.HasValue ? report.LunchStart.Value.ToString("HH:mm") : "00:00",
                        LunchEndTime = report.LunchEnd.HasValue ? report.LunchEnd.Value.ToString("HH:mm") : "00:00",
                        MissingTime = missingTimeMinutes == 540
                            ? (report.ReportDate < today ? "09:00" : "00:00") // 📌 **Geçmişse 9 saat, gelecekse 00:00**
                            : missingTimeMinutes > 0
                                ? TimeSpan.FromMinutes(missingTimeMinutes).ToString(@"hh\:mm")
                                : "00:00",
                        Overtime = overtimeMinutes == 0
                            ? "00:00"
                            : TimeSpan.FromMinutes(overtimeMinutes).ToString(@"hh\:mm"),
                        Status = report.Status
                    };
                }).ToList();

                ReportsDataGrid.ItemsSource = reportList;

                TotalMissingTime.Text = FormatToTotalHours(totalMissingTime);
                TotalOvertime.Text = FormatToTotalHours(totalOvertime);

                double totalNetTime = totalOvertime - totalMissingTime;

                TotalNetWorkDuration.Text = totalNetTime < 0
                    ? $"- {FormatToTotalHours(Math.Abs(totalNetTime))}"
                    : FormatToTotalHours(totalNetTime);
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

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void UserComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
