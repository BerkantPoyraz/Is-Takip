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
using WorkTrackingWpf;

namespace WorkTrackingWpf
{
    public partial class ReportsLog : Window
    {
        private readonly AppDbContext _context;

        public ReportsLog(AppDbContext context)
        {
            _context = context;
            InitializeComponent();
            DataContext = this;
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
        private async Task LoadUserReportsAsync(int year, int month)
        {
            try
            {
                if (App.CurrentUser == null)
                {
                    MessageBox.Show("Giriş yapılmadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var today = DateTime.Today;

                var userReports = await _context.UserReports
                    .Where(r => r.UserId == App.CurrentUser.UserId &&
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
                            if (report.ReportDate < today) // Sadece geçmiş günler için 9 saat eksik mesai ekle
                            {
                                missingTimeMinutes = 540; // 9 saat
                            }
                            else
                            {
                                missingTimeMinutes = 0; // Gelecek günlerde boş bırak
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
                            ? (report.ReportDate < today ? "09:00" : "00:00") // Geçmişse 9 saat, gelecekse 00:00
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


        private async void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedReports = ReportsDataGrid.SelectedItems.Cast<dynamic>().ToList();

            if (!selectedReports.Any())
            {
                MessageBox.Show("Lütfen en az bir satır seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                foreach (var report in selectedReports)
                {
                    int userReportsId = report.UserReportsId;

                    var dbReport = await _context.UserReports.FirstOrDefaultAsync(r => r.UserReportsId == userReportsId);
                    if (dbReport != null)
                    {
                        dbReport.Status = Status.Onay;
                    }
                }

                await _context.SaveChangesAsync();
                MessageBox.Show("Seçili satırlar onaylandı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);

                await ReloadDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReportErrorButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedReports = ReportsDataGrid.SelectedItems.Cast<dynamic>().ToList();

            if (!selectedReports.Any())
            {
                MessageBox.Show("Lütfen en az bir satır seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                foreach (var report in selectedReports)
                {
                    int userReportsId = report.UserReportsId;

                    var dbReport = await _context.UserReports.FirstOrDefaultAsync(r => r.UserReportsId == userReportsId);
                    if (dbReport != null)
                    {
                        dbReport.Status = Status.Hata;
                    }
                }

                await _context.SaveChangesAsync();
                MessageBox.Show("Seçilen satırlara hata bildirildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);

                await ReloadDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata bildirimi sırasında bir sorun oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task ReloadDataGrid()
        {
            if (YearComboBox.SelectedItem != null && MonthComboBox.SelectedItem != null)
            {
                int selectedYear = (int)YearComboBox.SelectedItem;
                int selectedMonth = (int)MonthComboBox.SelectedItem;
                await LoadUserReportsAsync(selectedYear, selectedMonth);
            }
        }
    }
}

//private async Task LoadUserReportsAsync(int year, int month)
//{
//    try
//    {
//        if (App.CurrentUser == null)
//        {
//            MessageBox.Show("Giriş yapılmadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
//            return;
//        }
//
//        var userReports = await _context.UserReports
//            .Where(r => r.UserId == App.CurrentUser.UserId &&
//                        r.ReportDate.Year == year &&
//                        r.ReportDate.Month == month)
//            .OrderBy(r => r.ReportDate) // Tarihe göre sıralama
//            .ToListAsync();
//
//        double totalMissingTime = 0;
//        double totalOvertime = 0;
//
//        var reportList = userReports.Select(report =>
//        {
//            double missingTimeMinutes = 0;
//            double overtimeMinutes = 0;
//
//            var workStart = report.WorkStart ?? DateTime.MinValue;
//            var workEnd = report.WorkEnd ?? DateTime.MinValue;
//
//            TimeSpan startOfWork = new TimeSpan(8, 0, 0);
//            TimeSpan lunchStart = new TimeSpan(13, 0, 0);
//            TimeSpan lunchEndDefault = new TimeSpan(14, 0, 0);
//            TimeSpan endOfWork = new TimeSpan(18, 0, 0);
//
//            var lunchDuration = lunchEndDefault - lunchStart;
//            var lunchEnd = report.LunchEnd?.TimeOfDay ?? lunchEndDefault;
//
//            if (report.ReportDate.DayOfWeek == DayOfWeek.Saturday || report.ReportDate.DayOfWeek == DayOfWeek.Sunday)
//            {
//                if (workStart != DateTime.MinValue && workEnd != DateTime.MinValue)
//                {
//                    var totalWorkTime = workEnd - workStart;
//
//                    if (workStart.TimeOfDay < lunchEnd && workEnd.TimeOfDay > lunchStart)
//                    {
//                        totalWorkTime -= lunchDuration;
//                    }
//
//                    overtimeMinutes += totalWorkTime.TotalMinutes;
//                }
//            }
//            else
//            {
//                if (workStart != DateTime.MinValue)
//                {
//                    // Fazla mesai: 08:00'dan önce giriş varsa fazla mesaiye ekle
//                    if (workStart.TimeOfDay < startOfWork)
//                    {
//                        overtimeMinutes += (startOfWork - workStart.TimeOfDay).TotalMinutes;
//                    }
//
//                    // Fazla mesai: 14:00'dan önce girildiyse ve çalışma saatine dahil değilse
//                    if (workStart.TimeOfDay > lunchStart && workStart.TimeOfDay < lunchEndDefault)
//                    {
//                        overtimeMinutes += (lunchEndDefault - workStart.TimeOfDay).TotalMinutes;
//                    }
//                }
//
//                if (lunchEnd > lunchEndDefault)
//                {
//                    missingTimeMinutes += (lunchEnd - lunchEndDefault).TotalMinutes;
//                }
//
//                if (workStart == DateTime.MinValue && workEnd == DateTime.MinValue)
//                {
//                    missingTimeMinutes = 540; // 9 saat
//                    overtimeMinutes = 0; // Fazla mesaiyi 0 olarak ayarla
//                }
//                else
//                {
//                    if (workStart.TimeOfDay > startOfWork && workStart.TimeOfDay < lunchStart)
//                    {
//                        missingTimeMinutes += (workStart.TimeOfDay - startOfWork).TotalMinutes;
//                    }
//                    if (workStart.TimeOfDay > lunchEnd && workStart.TimeOfDay < endOfWork)
//                    {
//                        missingTimeMinutes += (workStart.TimeOfDay - lunchEnd).TotalMinutes;
//                    }
//                    if (workEnd.TimeOfDay < endOfWork)
//                    {
//                        missingTimeMinutes += (endOfWork - workEnd.TimeOfDay).TotalMinutes;
//                    }
//
//                    if (workEnd.TimeOfDay > endOfWork)
//                    {
//                        overtimeMinutes += (workEnd.TimeOfDay - endOfWork).TotalMinutes;
//                    }
//
//                    // Kullanıcı öğlen 13:00 - 14:00 arası mola alması gerekirken erken dönerse (Örn: 13:30)
//                    if (report.LunchEnd.HasValue && report.LunchEnd.Value.TimeOfDay < lunchEndDefault)
//                    {
//                        double earlyReturnMinutes = (lunchEndDefault - report.LunchEnd.Value.TimeOfDay).TotalMinutes;
//                        overtimeMinutes += earlyReturnMinutes;
//                    }
//                }
//            }
//
//            if (missingTimeMinutes > 540)
//            {
//                missingTimeMinutes = 540;
//            }
//
//            totalMissingTime += missingTimeMinutes;
//            totalOvertime += overtimeMinutes;
//
//            return new
//            {
//                report.UserReportsId,
//                report.UserId,
//                WorkDate = report.ReportDate.ToString("dd.MM.yyyy dddd", new CultureInfo("tr-TR")),
//                WorkStartTime = workStart == DateTime.MinValue ? "00:00" : workStart.ToString("HH:mm"),
//                EndTime = workEnd == DateTime.MinValue ? "00:00" : workEnd.ToString("HH:mm"),
//                LunchStartTime = report.LunchStart.HasValue ? report.LunchStart.Value.ToString("HH:mm") : "00:00",
//                LunchEndTime = report.LunchEnd.HasValue ? report.LunchEnd.Value.ToString("HH:mm") : "00:00",
//                MissingTime = missingTimeMinutes == 540
//                    ? "09:00" // Eğer 9 saat eksik mesai varsa 09:00 olarak göster
//                    : missingTimeMinutes > 0
//                        ? TimeSpan.FromMinutes(missingTimeMinutes).ToString(@"hh\:mm")
//                        : "00:00",
//                Overtime = overtimeMinutes == 0
//                    ? "00:00" // Eğer fazla mesai yoksa 00:00 olarak göster
//                    : TimeSpan.FromMinutes(overtimeMinutes).ToString(@"hh\:mm"),
//                Status = report.Status
//            };
//        }).ToList();
//
//        ReportsDataGrid.ItemsSource = reportList;
//
//        TotalMissingTime.Text = FormatToTotalHours(totalMissingTime);
//        TotalOvertime.Text = FormatToTotalHours(totalOvertime);
//
//        double totalNetTime = totalOvertime - totalMissingTime;
//
//        TotalNetWorkDuration.Text = totalNetTime < 0
//            ? $"- {FormatToTotalHours(Math.Abs(totalNetTime))}"
//            : FormatToTotalHours(totalNetTime);
//    }
//    catch (Exception ex)
//    {
//        MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
//    }
//}