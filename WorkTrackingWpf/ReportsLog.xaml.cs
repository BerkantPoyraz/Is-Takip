using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
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

        // Pencere yüklendiğinde yıl ve ay seçimi için combo box'ları doldurur
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Yıl ComboBox'ına bugünün yılı eklensin
            YearComboBox.Items.Clear();
            for (int i = DateTime.Now.Year - 10; i <= DateTime.Now.Year + 10; i++)
            {
                YearComboBox.Items.Add(i);
            }
            YearComboBox.SelectedItem = DateTime.Now.Year;

            // Ay ComboBox'ına 1'den 12'ye kadar ay numaralarını ekle
            MonthComboBox.Items.Clear();
            for (int i = 1; i <= 12; i++)
            {
                MonthComboBox.Items.Add(i);
            }
            MonthComboBox.SelectedItem = DateTime.Now.Month;
        }

        // Filtreleme butonuna tıklanınca seçilen yıl ve ay'a göre çalışma loglarını getirir
        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (YearComboBox.SelectedItem != null && MonthComboBox.SelectedItem != null)
            {
                int selectedYear = (int)YearComboBox.SelectedItem;
                int selectedMonth = (int)MonthComboBox.SelectedItem;

                await LoadWorkLogsAsync(selectedYear, selectedMonth);
            }
            else
            {
                MessageBox.Show("Lütfen yıl ve ay seçiniz.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Çalışma loglarını yıl ve aya göre yükler
        private async Task LoadWorkLogsAsync(int year, int month)
        {
            try
            {
                var workLogs = await _context.WorkLog
                    .Where(w => w.StartTime.HasValue && w.StartTime.Value.Year == year && w.StartTime.Value.Month == month)
                    .ToListAsync();

                if (workLogs.Count == 0)
                {
                    MessageBox.Show("Veri bulunamadı.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Günlük çalışma süresi sabiti (9 saat = 540 dakika)
                const int dailyWorkMinutes = 540;

                double totalMissingTime = 0; // Toplam Eksik Mesai
                double totalOvertime = 0;    // Toplam Fazla Mesai

                // Gruplama ve aynı gün için kayıtları birleştirme
                var groupedLogs = workLogs
                    .GroupBy(w => w.StartTime.Value.Date) // Aynı gün olanları gruplandır
                    .Select(group =>
                    {
                        var minStart = group.Min(w => w.StartTime.Value); // En erken başlangıç
                        var maxEnd = group.Max(w => w.EndTime ?? DateTime.MinValue); // En geç bitiş

                        // Öğle arası toplam süresi
                        var totalLunchBreak = group.Sum(w =>
                            (w.LunchBreakStart.HasValue && w.LunchBreakEnd.HasValue)
                                ? (w.LunchBreakEnd.Value - w.LunchBreakStart.Value).TotalMinutes
                                : 0);

                        // Çalışma süresi hesaplama (Giriş ve Çıkış saatleri)
                        var workDuration = 0.0;
                        if (minStart != DateTime.MinValue && maxEnd != DateTime.MinValue)
                        {
                            workDuration = (maxEnd - minStart).TotalMinutes; // Çalışma süresi
                        }

                        // Fazla mesai ve eksik mesai hesaplama
                        double overtime = 0;
                        double missingTime = 0;

                        // Net çalışma süresi 9 saatten fazla ise fazla mesai hesapla
                        if (workDuration - totalLunchBreak > dailyWorkMinutes)
                        {
                            overtime = (workDuration - totalLunchBreak) - dailyWorkMinutes;
                        }
                        // Net çalışma süresi 9 saatten az ise eksik mesai hesapla
                        else if (workDuration - totalLunchBreak < dailyWorkMinutes)
                        {
                            missingTime = dailyWorkMinutes - (workDuration - totalLunchBreak);
                        }

                        // Toplam mesai saatlerini biriktir
                        totalMissingTime += missingTime;
                        totalOvertime += overtime;

                        return new
                        {
                            WorkDate = minStart.ToString("dd.MM.yyyy dddd", new System.Globalization.CultureInfo("tr-TR")),
                            WorkStartTime = minStart.ToString("HH:mm"),
                            EndTime = maxEnd == DateTime.MinValue ? "00:00" : maxEnd.ToString("HH:mm"),
                            LunchStartTime = group.FirstOrDefault(w => w.LunchBreakStart.HasValue)?.LunchBreakStart?.ToString("HH:mm") ?? "00:00",
                            LunchEndTime = group.FirstOrDefault(w => w.LunchBreakEnd.HasValue)?.LunchBreakEnd?.ToString("HH:mm") ?? "00:00",
                            MissingTime = TimeSpan.FromMinutes(missingTime > 0 ? missingTime : 0).ToString(@"hh\:mm"),
                            Overtime = TimeSpan.FromMinutes(overtime).ToString(@"hh\:mm")
                        };
                    })
                    .OrderBy(w => DateTime.Parse(w.WorkDate)) // Günlere göre sıralama
                    .ToList();

                // Verileri DataGrid'e bağla
                ReportsDataGrid.ItemsSource = groupedLogs;

                // Toplamları DataGrid alt kısmında göstermek için bir şekilde bind et veya textblock kullan
                TotalMissingTime.Text = TimeSpan.FromMinutes(totalMissingTime).ToString(@"hh\:mm");
                TotalOvertime.Text = TimeSpan.FromMinutes(totalOvertime).ToString(@"hh\:mm");

                // Toplam Net Mesai hesapla (Fazla Mesai - Eksik Mesai)
                double totalNetWork = totalOvertime - totalMissingTime; // Fazla mesai - Eksik mesai
                string netWorkText = totalNetWork < 0 ? "-" + TimeSpan.FromMinutes(Math.Abs(totalNetWork)).ToString(@"hh\:mm") : TimeSpan.FromMinutes(totalNetWork).ToString(@"hh\:mm");

                // Toplam net çalışma süresini yaz
                TotalNetWorkDuration.Text = netWorkText;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }






        private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ay seçimi değiştiğinde yapılacak işlemler
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ReportsDataGrid_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateRowBackgrounds();
    }

    // Satırları güncelleme metodu
    private void UpdateRowBackgrounds()
    {
        // Satırları almak için ItemContainerGenerator'ı kullanıyoruz
        foreach (var item in ReportsDataGrid.Items)
        {
            var row = (DataGridRow)ReportsDataGrid.ItemContainerGenerator.ContainerFromItem(item);
            if (row != null)
            {
                row.Background = new SolidColorBrush(Colors.White); // Başlangıçta beyaz yapıyoruz
            }
        }
    }

    // Onayla butonuna tıklayınca tüm satırları yeşil yapma
    private void ApproveButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in ReportsDataGrid.Items)
        {
            var row = (DataGridRow)ReportsDataGrid.ItemContainerGenerator.ContainerFromItem(item);
            if (row != null)
            {
                row.Background = new SolidColorBrush(Colors.Green); // Satırı yeşil yap
            }
        }
    }

    // Hata Bildir butonuna tıklayınca seçilen satırı kırmızı yapma
    private void ReportErrorButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedRow = ReportsDataGrid.SelectedItem as DataGridRow;
        if (selectedRow != null)
        {
            selectedRow.Background = new SolidColorBrush(Colors.Red); // Seçilen satırı kırmızı yapıyoruz
        }
    }
    }
}
