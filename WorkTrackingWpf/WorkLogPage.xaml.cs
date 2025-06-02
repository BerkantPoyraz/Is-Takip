using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;

namespace WorkTrackingWpf
{
    public partial class WorkLogPage : Page
    {
        private readonly AppDbContext _context;
        private List<WorkLog> _workLogs;

        public WorkLogPage(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            Loaded += WorkLogPage_Loaded;
        }

        private async void WorkLogPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUsers();
            await LoadProjects();
            await LoadTasks();
        }

        private async Task LoadUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new { u.UserId, FullName = u.FirstName + " " + u.LastName })
                    .ToListAsync();

                users.Insert(0, new { UserId = 0, FullName = "Tümü" }); // 📌 "Tümü" Seçeneğini Ekle

                UserFilterComboBox.ItemsSource = users;
                UserFilterComboBox.DisplayMemberPath = "FullName";
                UserFilterComboBox.SelectedValuePath = "UserId";
                UserFilterComboBox.SelectedIndex = 0; // Varsayılan olarak "Tümü" seçili
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kullanıcılar yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadProjects()
        {
            try
            {
                var projects = await _context.Projects
                    .Select(p => new { p.ProjectId, p.ProjectName })
                    .ToListAsync();

                projects.Insert(0, new { ProjectId = 0, ProjectName = "Tümü" }); // 📌 "Tümü" Seçeneğini Ekle

                ProjectFilterComboBox.ItemsSource = projects;
                ProjectFilterComboBox.DisplayMemberPath = "ProjectName";
                ProjectFilterComboBox.SelectedValuePath = "ProjectId";
                ProjectFilterComboBox.SelectedIndex = 0; // Varsayılan olarak "Tümü" seçili
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Projeler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadTasks()
        {
            try
            {
                var tasks = await _context.Tasks
                    .Select(t => new { t.TaskId, t.TaskName })
                    .ToListAsync();

                tasks.Insert(0, new { TaskId = 0, TaskName = "Tümü" }); // 📌 "Tümü" Seçeneğini Ekle

                TaskFilterComboBox.ItemsSource = tasks;
                TaskFilterComboBox.DisplayMemberPath = "TaskName";
                TaskFilterComboBox.SelectedValuePath = "TaskId";
                TaskFilterComboBox.SelectedIndex = 0; // Varsayılan olarak "Tümü" seçili
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Görevler yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task LoadWorkLogs()
        {
            try
            {
                _workLogs = await _context.WorkLog
                    .Include(w => w.Project)
                    .Include(w => w.ProjectTask.Task)
                    .Include(w => w.User)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İş kayıtları yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateWorkLogGrid(IEnumerable<WorkLog> workLogs)
        {
            var formattedLogs = workLogs
                .OrderBy(w => w.StartTime) // Tarih sırasına göre sıralama
                .Select(w => new
                {
                    Tarih = w.StartTime?.ToString("dddd, dd MMMM yyyy", new CultureInfo("tr-TR")) ?? "-",
                    FullName = w.User.FirstName + " " + w.User.LastName,
                    ProjectName = w.Project.ProjectName,
                    TaskName = w.ProjectTask?.Task.TaskName ?? "-",
                    StartTime = w.StartTime?.ToString("HH:mm") ?? "-",
                    EndTime = w.EndTime?.ToString("HH:mm") ?? "-",
                    TotalDuration = w.EndTime.HasValue && w.StartTime.HasValue
                        ? $"{(int)(w.EndTime - w.StartTime)?.TotalHours} sa {((w.EndTime - w.StartTime)?.Minutes ?? 0)} dk"
                        : "-"
                }).ToList();

            WorkLogDataGrid.ItemsSource = formattedLogs;
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T foundChild)
                    return foundChild;

                T childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }


        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filteredLogs = _context.WorkLog
                    .Include(w => w.Project)
                    .Include(w => w.ProjectTask.Task)
                    .Include(w => w.User)
                    .AsQueryable();

                // Kullanıcı filtresi
                if (UserFilterComboBox.SelectedValue is int userId && userId > 0)
                {
                    filteredLogs = filteredLogs.Where(w => w.UserId == userId);
                }

                // Proje filtresi
                if (ProjectFilterComboBox.SelectedValue is int projectId && projectId > 0)
                {
                    filteredLogs = filteredLogs.Where(w => w.ProjectId == projectId);
                }

                // Görev filtresi
                if (TaskFilterComboBox.SelectedValue is int taskId && taskId > 0)
                {
                    filteredLogs = filteredLogs.Where(w => w.ProjectTask.TaskId == taskId);
                }

                // 📅 **Tarih filtresi** (Eğer bir tarih seçildiyse sadece o tarihi getir)
                if (DateFilterPicker.SelectedDate.HasValue)
                {
                    DateTime selectedDate = DateFilterPicker.SelectedDate.Value.Date;
                    filteredLogs = filteredLogs.Where(w => w.StartTime.HasValue && w.StartTime.Value.Date == selectedDate);
                }

                // 📆 **Ay filtresi** (Eğer bir ay seçildiyse sadece o ayın verilerini getir)
                if (MonthFilterComboBox.SelectedValue is ComboBoxItem selectedMonthItem && int.TryParse(selectedMonthItem.Tag.ToString(), out int selectedMonth) && selectedMonth > 0)
                {
                    filteredLogs = filteredLogs.Where(w => w.StartTime.HasValue && w.StartTime.Value.Month == selectedMonth);
                }

                // Filtrelenmiş sonuçları al
                var workLogs = await filteredLogs.ToListAsync();

                if (!workLogs.Any())
                {
                    MessageBox.Show("Seçilen kriterlere uygun kayıt bulunamadı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Grid'i güncelle
                UpdateWorkLogGrid(workLogs);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Filtreleme sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void WorkLogDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = FindParent<ScrollViewer>(WorkLogDataGrid);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);
                e.Handled = true;
            }
        }

        // Parent ScrollViewer'ı bulmak için yardımcı fonksiyon
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }


    }
}
