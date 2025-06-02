using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkTracking.DAL.Data;
using WorkTracking.DAL.Repositories;
using WorkTracking.Model.Model;

namespace WorkTrackingWpf
{
    public partial class ProjectSelectionPage : Window
    {
        private readonly IUserRepository _userRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly User _loggedInUser;
        private DispatcherTimer _timeCheckTimer;
        private readonly AppDbContext _context;

        public ProjectSelectionPage(IProjectRepository projectRepository, IUserRepository userRepository, User loggedInUser)
        {
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _loggedInUser = loggedInUser;
            _context = App.ServiceProvider.GetRequiredService<AppDbContext>();

            InitializeComponent();

            if (_loggedInUser.Role == UserRole.Admin)
            {
                AdminPanelButton.Visibility = Visibility.Visible;
            }

            LoadProjectsAsync();
            StartTimeCheckTimer();
        }
        private async void LoadProjectsAsync()
        {
            try
            {
                var projects = await _projectRepository.GetProjectsAsync();
                var activeProjects = projects
                    .Where(p => p.ProjectStatus == ProjectStatus.Aktif)
                    .Select(project => new
                    {
                        ProjectId = project.ProjectId,
                        DisplayName = $"{project.ProjectNumber} - {project.ProjectName}"
                    })
                    .ToList();

                ProjectsComboBox.ItemsSource = activeProjects;
                ProjectsComboBox.DisplayMemberPath = "DisplayName";
                ProjectsComboBox.SelectedValuePath = "ProjectId";

                ProjectsComboBox.SelectionChanged += async (sender, args) =>
                {
                    var selectedProject = ProjectsComboBox.SelectedItem as dynamic;
                    if (selectedProject != null)
                    {
                        int selectedProjectId = selectedProject.ProjectId;
                        var user = GetLoggedInUser();

                        using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
                        {
                            var existingWorkLog = await dbContext.WorkLog
                                .Where(wl => wl.UserId == user.UserId &&
                                             wl.ProjectId == selectedProjectId &&
                                             wl.EndTime == null)
                                .OrderByDescending(wl => wl.StartTime)
                                .FirstOrDefaultAsync();

                            if (existingWorkLog != null)
                            {
                                existingWorkLog.EndTime = DateTime.Now;
                                await dbContext.SaveChangesAsync();
                            }
                        }

                        var projectTasks = await _projectRepository.GetProjectTasksByProjectIdAsync(selectedProjectId);
                        if (projectTasks != null && projectTasks.Any())
                        {
                            var tasksWithDetails = projectTasks.Select(pt => new
                            {
                                pt.ProjectTaskId,
                                TaskName = pt.Task.TaskName
                            }).ToList();

                            ProjectTasksComboBox.ItemsSource = tasksWithDetails;
                            ProjectTasksComboBox.DisplayMemberPath = "TaskName";
                            ProjectTasksComboBox.SelectedValuePath = "ProjectTaskId";
                        }
                        else
                        {
                            ProjectTasksComboBox.ItemsSource = null;
                        }
                    }
                };

                ProjectTasksComboBox.SelectionChanged += async (sender, args) =>
                {
                    var selectedTask = ProjectTasksComboBox.SelectedItem as dynamic;
                    if (selectedTask != null)
                    {
                        int selectedTaskId = selectedTask.ProjectTaskId;
                        var user = GetLoggedInUser();

                        using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
                        {
                            var existingWorkLog = await dbContext.WorkLog
                                .Where(wl => wl.UserId == user.UserId &&
                                             wl.ProjectTaskId == selectedTaskId &&
                                             wl.EndTime == null)
                                .OrderByDescending(wl => wl.StartTime)
                                .FirstOrDefaultAsync();

                            if (existingWorkLog != null)
                            {
                                existingWorkLog.EndTime = DateTime.Now;
                                await dbContext.SaveChangesAsync();
                            }
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Proje yükleme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            var activeWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive);

            if (activeWindow != null)
            {
                activeWindow.WindowState = WindowState.Minimized;
            }

            var adminPanel = new AdminPanel
            {
                Topmost = true
            };
            adminPanel.Show();

            adminPanel.Closed += (s, args) =>
            {
                if (activeWindow != null)
                {
                    activeWindow.WindowState = WindowState.Normal;
                }
            };
        }
        private User GetLoggedInUser()
        {
            return _loggedInUser;
        }
        private void BringWindowToFront()
        {
            // Eğer pencere gizliyse, tekrar göster
            if (!this.IsVisible)
            {
                this.Show();
                this.ShowInTaskbar = true;
            }

            // Eğer minimize edilmişse, normal hale getir
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }

            // Odağı al ve en öne getir
            this.Activate();
            this.Topmost = true;
        }

        private void StartTimeCheckTimer()
        {
            _timeCheckTimer = new DispatcherTimer();
            _timeCheckTimer.Interval = TimeSpan.FromSeconds(1);
            _timeCheckTimer.Tick += async (sender, args) =>
            {
                DateTime currentTime = DateTime.Now;

                if (currentTime.Hour == 13 && currentTime.Minute == 0 && currentTime.Second == 0)
                {
                    BringWindowToFront();
                    await UpdateLunchStartTime(currentTime);
                }
                if (currentTime.Hour == 17 && currentTime.Minute == 0 && currentTime.Second == 0)
                {
                    BringWindowToFront();
                }
                if (currentTime.Hour == 18 && currentTime.Minute == 0 && currentTime.Second == 0)
                {
                    BringWindowToFront();
                    await UpdateWorkEndTime(currentTime);
                }
            };
            _timeCheckTimer.Start();
        }

        private async Task UpdateWorkEndTime(DateTime currentTime)
        {
            using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                try
                {
                    var userReport = await dbContext.UserReports
                        .FirstOrDefaultAsync(ur => ur.UserId == _loggedInUser.UserId &&
                                                   ur.ReportDate.Date == currentTime.Date);

                    if (userReport != null && userReport.WorkEnd == null)
                    {
                        userReport.WorkEnd = currentTime;
                        await dbContext.SaveChangesAsync();
                        MessageBox.Show("Çalışma günü sona erdi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"WorkEnd hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task HandleWorkStart(DateTime currentTime)
        {
            using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                try
                {
                    var userReport = await dbContext.UserReports
                        .FirstOrDefaultAsync(ur => ur.UserId == _loggedInUser.UserId && ur.ReportDate.Date == currentTime.Date);

                    if (userReport != null)
                    {
                        if (userReport.LunchStart != null && userReport.LunchEnd == null)
                        {
                            userReport.LunchEnd = currentTime;

                            await dbContext.SaveChangesAsync();

                            // MessageBox.Show($"Moladan dönüş saati {currentTime} olarak kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }

                    var newWorkLog = new WorkLog
                    {
                        UserId = _loggedInUser.UserId,
                        StartTime = currentTime
                    };

                    dbContext.WorkLog.Add(newWorkLog);
                    await dbContext.SaveChangesAsync();

                    // MessageBox.Show($"Çalışma {currentTime} tarihinde başlatıldı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"WorkStart hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async Task UpdateLunchStartTime(DateTime currentTime)
        {
            using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                try
                {
                    var userReport = await dbContext.UserReports
                        .FirstOrDefaultAsync(ur => ur.UserId == _loggedInUser.UserId && ur.ReportDate.Date == currentTime.Date);

                    if (userReport != null)
                    {
                        // LunchStart kontrolü: Eğer 00:00 ise güncelle
                        if (!userReport.LunchStart.HasValue || userReport.LunchStart.Value.TimeOfDay == TimeSpan.Zero)
                        {
                            userReport.LunchStart = currentTime;

                            var activeWorkLog = await dbContext.WorkLog
                                .FirstOrDefaultAsync(wl => wl.UserId == _loggedInUser.UserId && wl.EndTime == null);

                            if (activeWorkLog != null)
                            {
                                activeWorkLog.EndTime = currentTime;
                            }

                            await dbContext.SaveChangesAsync();
                           // MessageBox.Show($"Öğlen molası başlangıcı {currentTime:HH:mm} olarak kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"LunchStart güncelleme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task UpdateLunchEndTime(DateTime currentTime)
        {
            using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                try
                {
                    var userReport = await dbContext.UserReports
                        .FirstOrDefaultAsync(ur => ur.UserId == _loggedInUser.UserId && ur.ReportDate.Date == currentTime.Date);

                    if (userReport != null)
                    {
                        // LunchEnd kontrolü: Eğer LunchStart dolu ve LunchEnd 00:00 ise güncelle
                        if (userReport.LunchStart.HasValue && (!userReport.LunchEnd.HasValue || userReport.LunchEnd.Value.TimeOfDay == TimeSpan.Zero))
                        {
                            userReport.LunchEnd = currentTime;
                            await dbContext.SaveChangesAsync();
                           // MessageBox.Show($"Öğlen molası bitişi {currentTime:HH:mm} olarak kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else if (userReport.LunchEnd.HasValue && userReport.LunchEnd.Value.TimeOfDay != TimeSpan.Zero)
                        {
                            MessageBox.Show("Mola bitiş saati zaten kaydedilmiş.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"LunchEnd güncelleme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void WorkConfirm(object sender, RoutedEventArgs e)
        {
            // 1. Ana pencereyi gizle ve sistem tepsisinde göster
            var activeWindow = Application.Current.MainWindow;
            if (activeWindow != null)
            {
                activeWindow.Hide(); // Pencereyi tamamen gizler
            }

            // 2. TrayIcon'u görünür yap
            TrayIcon.Visibility = Visibility.Visible;

            // 3. Proje Seçim Penceresi açıksa, onu da gizle
            var projectSelectionWindow = Application.Current.Windows.OfType<ProjectSelectionPage>().FirstOrDefault();
            if (projectSelectionWindow != null)
            {
                projectSelectionWindow.Hide();
            }

            // 4. ReportsLog penceresini aç
            var reportsLogWindow = new ReportsLog(App.ServiceProvider.GetRequiredService<AppDbContext>());
            reportsLogWindow.Topmost = true;
            reportsLogWindow.Show();
        }


        private async void WorkStart(object sender, RoutedEventArgs e)
        {
            DateTime currentTime = DateTime.Now;

            var selectedProject = ProjectsComboBox.SelectedItem as dynamic;
            var selectedTask = ProjectTasksComboBox.SelectedItem as dynamic;

            if (selectedProject == null)
            {
                MessageBox.Show("Lütfen bir proje seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (selectedTask == null)
            {
                MessageBox.Show("Lütfen bir görev seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var user = GetLoggedInUser();
            if (user == null)
            {
                MessageBox.Show("Kullanıcı bilgisi bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                try
                {
                    // Bugünkü rapor kontrolü ve oluşturma
                    var todayReport = await dbContext.UserReports
                        .FirstOrDefaultAsync(r => r.UserId == user.UserId && r.ReportDate.Date == currentTime.Date);

                    if (todayReport == null)
                    {
                        // Yeni rapor oluşturuluyor
                        todayReport = new UserReports
                        {
                            UserId = user.UserId,
                            ReportDate = currentTime.Date,
                            WorkStart = currentTime, // Sabah giriş saati
                            WorkEnd = null,
                            LunchStart = null,
                            LunchEnd = null,
                            IdleTime = TimeSpan.Zero,
                            Status = Status.Seçiniz
                        };
                        dbContext.UserReports.Add(todayReport);
                    }
                    else
                    {
                        // Sabah giriş yapılmamışsa kaydet
                        if (!todayReport.WorkStart.HasValue || todayReport.WorkStart.Value.TimeOfDay == TimeSpan.Zero)
                        {
                            todayReport.WorkStart = currentTime;
                        }

                        // Öğle molası kontrolü
                        if (todayReport.LunchStart.HasValue &&
                            todayReport.LunchStart.Value.TimeOfDay != TimeSpan.Zero &&
                            (!todayReport.LunchEnd.HasValue || todayReport.LunchEnd.Value.TimeOfDay == TimeSpan.Zero))
                        {
                            todayReport.LunchEnd = currentTime;
                        }
                    }

                    // Eksik günleri ekle
                    var firstDayOfMonth = new DateTime(currentTime.Year, currentTime.Month, 1);
                    var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                    var allDaysInMonth = Enumerable.Range(0, (lastDayOfMonth - firstDayOfMonth).Days + 1)
                        .Select(offset => firstDayOfMonth.AddDays(offset))
                        .Where(day => day.Date != currentTime.Date)
                        .ToList();

                    var existingReports = await dbContext.UserReports
                        .Where(r => r.UserId == user.UserId &&
                                    r.ReportDate >= firstDayOfMonth &&
                                    r.ReportDate <= lastDayOfMonth)
                        .Select(r => r.ReportDate.Date)
                        .ToListAsync();

                    foreach (var day in allDaysInMonth)
                    {
                        if (!existingReports.Contains(day.Date))
                        {
                            dbContext.UserReports.Add(new UserReports
                            {
                                UserId = user.UserId,
                                ReportDate = day,
                                WorkStart = null,
                                WorkEnd = null,
                                LunchStart = null,
                                LunchEnd = null,
                                IdleTime = TimeSpan.Zero,
                                Status = Status.Seçiniz
                            });
                        }
                    }

                    // Aktif WorkLog varsa kapat
                    var activeWorkLog = await dbContext.WorkLog
                        .Where(wl => wl.UserId == user.UserId && wl.EndTime == null)
                        .OrderByDescending(wl => wl.StartTime)
                        .FirstOrDefaultAsync();

                    if (activeWorkLog != null)
                    {
                        activeWorkLog.EndTime = currentTime;
                    }

                    // Yeni WorkLog kaydı oluştur
                    var newWorkLog = new WorkLog
                    {
                        UserId = user.UserId,
                        ProjectId = selectedProject.ProjectId,
                        ProjectTaskId = selectedTask.ProjectTaskId,
                        StartTime = currentTime
                    };
                    dbContext.WorkLog.Add(newWorkLog);

                    // Değişiklikleri kaydet
                    await dbContext.SaveChangesAsync();

                    MessageBox.Show("Çalışma başlatıldı", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Hide();
                    TrayIcon.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Çalışma başlatma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
        {
            ShowApp();
        }

        private void ShowApp_Click(object sender, RoutedEventArgs e)
        {
            ShowApp();
        }

        private void ShowApp()
        {
            this.Show();
            this.WindowState = WindowState.Maximized;
            this.Topmost = true;
            this.Activate();
            TrayIcon.Visibility = Visibility.Collapsed;
        }

        private async void WorkEnd(object sender, RoutedEventArgs e)
        {
            var selectedProject = ProjectsComboBox.SelectedItem as dynamic;
            var selectedTask = ProjectTasksComboBox.SelectedItem as dynamic;

            // Eğer proje veya görev seçili değilse programı kapat
            if (selectedProject == null || selectedTask == null)
            {
                Application.Current.Shutdown();
                return;
            }

            DateTime currentTime = DateTime.Now;

            var user = GetLoggedInUser();
            if (user == null)
            {
                MessageBox.Show("Kullanıcı bilgisi bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                try
                {
                    var activeWorkLog = await dbContext.WorkLog
                        .Where(wl => wl.UserId == user.UserId && wl.EndTime == null)
                        .OrderByDescending(wl => wl.StartTime)
                        .FirstOrDefaultAsync();

                    if (activeWorkLog != null)
                    {
                        activeWorkLog.EndTime = currentTime;
                    }

                    var userReport = await dbContext.UserReports
                        .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.ReportDate.Date == currentTime.Date);

                    if (userReport != null)
                    {
                        userReport.WorkEnd = currentTime;
                    }

                    await dbContext.SaveChangesAsync();

                    MessageBox.Show("Çalışma sonlandırıldı", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);

                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Çalışma sonlandırma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
       
        private void UpdateProjectAndTask(object sender, RoutedEventArgs e)
        {
            LoadProjects();

            if (ProjectsComboBox.SelectedValue != null)
            {
                int selectedProjectId = (int)ProjectsComboBox.SelectedValue;
                LoadTasksForProject(selectedProjectId);
            }
            else
            {
                ProjectTasksComboBox.ItemsSource = null;
            }

            MessageBox.Show("Projeler ve görevler başarıyla güncellendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void LoadProjects()
        {
            var projects = _context.Projects
                .Select(p => new
                {
                    p.ProjectId,
                    DisplayName = $"{p.ProjectNumber} - {p.ProjectName}"
                })
                .ToList();

            ProjectsComboBox.ItemsSource = projects;
            ProjectsComboBox.DisplayMemberPath = "DisplayName";
            ProjectsComboBox.SelectedValuePath = "ProjectId";
        }

        private void LoadTasksForProject(int projectId)
        {
            var tasks = _context.ProjectTasks
                .Where(pt => pt.ProjectId == projectId)
                .Include(pt => pt.Task)
                .Select(pt => new { pt.Task.TaskId, TaskName = pt.Task.TaskName })
                .ToList();

            ProjectTasksComboBox.ItemsSource = tasks;
            ProjectTasksComboBox.DisplayMemberPath = "TaskName";
            ProjectTasksComboBox.SelectedValuePath = "TaskId";
        }

        private void ChangePassword(object sender, RoutedEventArgs e)
        {
            var changePasswordWindow = new ChangesPasswordWindow(_userRepository, _loggedInUser);
            changePasswordWindow.ShowDialog();
        }

        private void ProjectsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ProjectTasksComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        //private void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        //{
        //    if (sender is ComboBox comboBox && comboBox.Items.Count > 0)
        //    {
        //        int newIndex = comboBox.SelectedIndex;
        //
        //        if (e.Delta > 0)
        //            newIndex--;
        //        else if (e.Delta < 0)
        //            newIndex++;
        //
        //        if (newIndex >= 0 && newIndex < comboBox.Items.Count)
        //            comboBox.SelectedIndex = newIndex;
        //
        //        e.Handled = true;
        //    }
        //}

        //private async Task UpdateIdleTime(DateTime currentTime)
        //{
        //    using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
        //    {
        //        try
        //        {
        //            var activeWorkLog = await dbContext.WorkLog
        //                .Where(wl => wl.UserId == _loggedInUser.UserId && wl.EndTime != null)
        //                .OrderByDescending(wl => wl.EndTime)
        //                .FirstOrDefaultAsync();
        //
        //            if (activeWorkLog != null)
        //            {
        //                var lastExitTime = activeWorkLog.EndTime.Value;
        //                var idleTime = currentTime - lastExitTime;
        //
        //                var userReport = await dbContext.UserReports
        //                    .FirstOrDefaultAsync(ur => ur.UserId == _loggedInUser.UserId && ur.ReportDate.Date == currentTime.Date);
        //
        //                if (userReport != null)
        //                {
        //                    if (userReport.IdleTime.HasValue)
        //                        userReport.IdleTime += idleTime;
        //                    else
        //                        userReport.IdleTime = idleTime;
        //                }
        //                else
        //                {
        //                    userReport = new UserReports
        //                    {
        //                        UserId = _loggedInUser.UserId,
        //                        ReportDate = currentTime.Date,
        //                        IdleTime = idleTime,
        //                        WorkStart = activeWorkLog?.StartTime ?? currentTime,
        //                    };
        //                    dbContext.UserReports.Add(userReport);
        //                }
        //                await dbContext.SaveChangesAsync();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //           // MessageBox.Show($"IdleTime hesaplama hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //    }
        //}
    }
}
