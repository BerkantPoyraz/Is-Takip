using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkTracking.DAL.Data;
using WorkTracking.DAL.Repositories;
using WorkTracking.Model.Model;
using WorkTrackingWpf;

namespace WorkTrackingWpf
{
    public partial class ProjectSelectionPage : Window
    {
        private readonly IUserRepository _userRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly User _loggedInUser;
        private DispatcherTimer _timeCheckTimer;

        public ProjectSelectionPage(IProjectRepository projectRepository, IUserRepository userRepository, User loggedInUser)
        {
            _projectRepository = projectRepository;
            _userRepository = userRepository;
            _loggedInUser = loggedInUser;

            InitializeComponent();

            if (_loggedInUser.Role == UserRole.Admin)
            {
                AdminPanelButton.Visibility = Visibility.Visible;
            }

            LoadProjectsAsync();
            StartTimeCheckTimer();

            this.Topmost = true;
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
            var activeWindow = Application.Current.MainWindow;
            if (activeWindow != null)
            {
                activeWindow.WindowState = WindowState.Minimized;
            }
            var projectSelectionWindow = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.Content is ProjectSelectionPage);
            if (projectSelectionWindow != null)
            {
                projectSelectionWindow.WindowState = WindowState.Minimized;
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

                if (projectSelectionWindow != null)
                {
                    projectSelectionWindow.WindowState = WindowState.Normal;
                }
            };
        }
        private User GetLoggedInUser()
        {
            return _loggedInUser;
        }
        private void BringWindowToFront()
        {
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Topmost = true;
            this.Topmost = true;
            this.Topmost = true;
        }
        private void StartTimeCheckTimer()
        {
            _timeCheckTimer = new DispatcherTimer();
            _timeCheckTimer.Interval = TimeSpan.FromSeconds(1);
            _timeCheckTimer.Tick += async (sender, args) =>
            {
                DateTime currentTime = DateTime.Now;

                if (currentTime.Hour == 14 && currentTime.Minute == 56 && currentTime.Second == 0)
                {
                    BringWindowToFront();
                    await UpdateLunchStartTime(currentTime);
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

                    if (userReport != null && userReport.LunchStart == null)
                    {
                        userReport.LunchStart = currentTime;

                        var activeWorkLog = await dbContext.WorkLog
                            .FirstOrDefaultAsync(wl => wl.UserId == _loggedInUser.UserId && wl.EndTime == null);

                        if (activeWorkLog != null)
                        {
                            activeWorkLog.EndTime = currentTime;
                        }

                        await dbContext.SaveChangesAsync();
                        //  MessageBox.Show($"Öğlen molası {currentTime} saatinde başlatıldı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // MessageBox.Show("Zaten bir mola başlatılmış.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"LunchStart hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        if (userReport.LunchStart != null && userReport.LunchEnd == null)
                        {
                            userReport.LunchEnd = currentTime;

                            await dbContext.SaveChangesAsync();

                            // MessageBox.Show($"Moladan dönüş saati {currentTime} olarak kaydedildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else if (userReport.LunchEnd != null)
                        {
                            // MessageBox.Show("Mola bitiş saati zaten kaydedilmiş.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Bu gün için herhangi bir kullanıcı raporu bulunamadı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"LunchEnd hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void WorkConfirm(object sender, RoutedEventArgs e)
        {
            var activeWindow = Application.Current.MainWindow;
            if (activeWindow != null)
            {
                activeWindow.WindowState = WindowState.Minimized;
            }

            var projectSelectionWindow = Application.Current.Windows.OfType<ProjectSelectionPage>().FirstOrDefault();
            if (projectSelectionWindow != null)
            {
                projectSelectionWindow.WindowState = WindowState.Minimized;
            }

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
                    
                    await UpdateIdleTime(currentTime);

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
                        if (userReport.LunchStart != null && userReport.LunchEnd == null)
                        {
                            userReport.LunchEnd = currentTime;
                        }
                    }
                    else
                    {
                        userReport = new UserReports
                        {
                            UserId = user.UserId,
                            ReportDate = currentTime.Date,
                            WorkStart = currentTime,
                            WorkEnd = null,
                            LunchStart = null,
                            LunchEnd = null,
                            IdleTime = TimeSpan.Zero,
                            Status = Status.Seçiniz
                        };
                        dbContext.UserReports.Add(userReport);
                    }

                    var newWorkLog = new WorkLog
                    {
                        UserId = user.UserId,
                        ProjectId = selectedProject.ProjectId,
                        ProjectTaskId = selectedTask.ProjectTaskId,
                        StartTime = currentTime,
                    };
                    dbContext.WorkLog.Add(newWorkLog);

                    await dbContext.SaveChangesAsync();

                    this.WindowState = WindowState.Minimized;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Çalışma başlatma hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async void WorkEnd(object sender, RoutedEventArgs e)
        {
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

                    if (userReport == null)
                    {
                        userReport = new UserReports
                        {
                            UserId = user.UserId,
                            ReportDate = currentTime.Date,
                            WorkStart = activeWorkLog?.StartTime ?? currentTime,
                            WorkEnd = currentTime,
                            IdleTime = TimeSpan.Zero
                        };
                        await dbContext.UserReports.AddAsync(userReport);
                    }
                    else
                    {
                        userReport.WorkEnd = currentTime;
                    }

                    await dbContext.SaveChangesAsync();

                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"WorkEnd hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private async Task UpdateIdleTime(DateTime currentTime)
        {
            using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                try
                {
                    var activeWorkLog = await dbContext.WorkLog
                        .Where(wl => wl.UserId == _loggedInUser.UserId && wl.EndTime != null)
                        .OrderByDescending(wl => wl.EndTime)
                        .FirstOrDefaultAsync();

                    if (activeWorkLog != null)
                    {
                        var lastExitTime = activeWorkLog.EndTime.Value;
                        var idleTime = currentTime - lastExitTime;

                        var userReport = await dbContext.UserReports
                            .FirstOrDefaultAsync(ur => ur.UserId == _loggedInUser.UserId && ur.ReportDate.Date == currentTime.Date);

                        if (userReport != null)
                        {
                            if (userReport.IdleTime.HasValue)
                                userReport.IdleTime += idleTime;
                            else
                                userReport.IdleTime = idleTime;
                        }
                        else
                        {
                            userReport = new UserReports
                            {
                                UserId = _loggedInUser.UserId,
                                ReportDate = currentTime.Date,
                                IdleTime = idleTime,
                                WorkStart = activeWorkLog?.StartTime ?? currentTime,
                            };
                            dbContext.UserReports.Add(userReport);
                        }

                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                   // MessageBox.Show($"IdleTime hesaplama hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }
    }
}
