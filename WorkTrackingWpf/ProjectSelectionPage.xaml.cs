using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkTracking.Model.Model;
using WorkTracking.DAL.Repositories;
using WorkTracking.DAL.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace WorkTrackingWpf
{
    public partial class ProjectSelectionPage : Window
    {
        private IUserRepository _userRepository;
        private IProjectRepository _projectRepository;
        private User _loggedInUser;

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
        }

        private async void LoadProjectsAsync()
        {
            try
            {
                var projects = await _projectRepository.GetProjectsAsync();
                var projectsWithNumber = projects.Select(project => new
                {
                    ProjectId = project.ProjectId,
                    DisplayName = $"{project.ProjectNumber} - {project.ProjectName}"
                }).ToList();

                ProjectsComboBox.ItemsSource = projectsWithNumber;
                ProjectsComboBox.DisplayMemberPath = "DisplayName";
                ProjectsComboBox.SelectedValuePath = "ProjectId";

                ProjectsComboBox.SelectionChanged += async (sender, args) =>
                {
                    var selectedProject = ProjectsComboBox.SelectedItem as dynamic;
                    if (selectedProject != null)
                    {
                        var projectTasks = await _projectRepository.GetProjectTasksByProjectIdAsync(selectedProject.ProjectId);
                        if (projectTasks != null && projectTasks.Count > 0)
                        {
                            ProjectTasksComboBox.ItemsSource = projectTasks;
                            ProjectTasksComboBox.DisplayMemberPath = "TaskName";
                            ProjectTasksComboBox.SelectedValuePath = "ProjectTaskId";
                        }
                        else
                        {
                            ProjectTasksComboBox.ItemsSource = null;
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}");
            }
        }

        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            AdminPanel adminPanel = new AdminPanel();
            adminPanel.Show();
        }

        private User GetLoggedInUser()
        {
            return _loggedInUser;  // Giriş yapan kullanıcıyı döndür
        }

        private async void WorkStart(object sender, RoutedEventArgs e)
        {
            var selectedProject = ProjectsComboBox.SelectedItem as dynamic;
            var selectedTask = ProjectTasksComboBox.SelectedItem as ProjectTask;

            if (selectedProject == null || selectedTask == null)
            {
                MessageBox.Show("Lütfen bir proje ve görev seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DateTime startTime = DateTime.Now;
            DateTime lunchBreakStart = DateTime.MinValue;
            DateTime lunchBreakEnd = DateTime.MinValue;

            var user = GetLoggedInUser();  // Giriş yapan kullanıcıyı al

            if (user == null)
            {
                MessageBox.Show("Kullanıcı bilgisi bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var workLog = new WorkLog
            {
                UserId = user.UserId,
                ProjectId = selectedProject.ProjectId,
                ProjectTaskId = selectedTask.ProjectTaskId,
                StartTime = startTime,
                LunchBreakStart = lunchBreakStart,
                LunchBreakEnd = lunchBreakEnd,
            };

            using (var dbContext = App.ServiceProvider.GetRequiredService<AppDbContext>())
            {
                try
                {
                    dbContext.WorkLog.Add(workLog);
                    await dbContext.SaveChangesAsync();
                    MessageBox.Show("Çalışma başlatıldı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private async void WorkEnd(object sender, RoutedEventArgs e)
        {
            var selectedProject = ProjectsComboBox.SelectedItem as dynamic;
            var selectedTask = ProjectTasksComboBox.SelectedItem as ProjectTask;

            if (selectedProject == null || selectedTask == null)
            {
                MessageBox.Show("Lütfen bir proje ve görev seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    var selectedProjectId = (int)selectedProject.ProjectId;
                    var selectedTaskId = (int)selectedTask.ProjectTaskId;

                    var workLog = await dbContext.WorkLog
                        .FirstOrDefaultAsync(wl =>
                            wl.UserId == user.UserId &&
                            wl.ProjectId == selectedProjectId &&
                            wl.ProjectTaskId == selectedTaskId &&
                            wl.EndTime == null);

                    if (workLog == null)
                    {
                        MessageBox.Show("Açık bir çalışma kaydı bulunamadı.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    workLog.EndTime = DateTime.Now;

                    if (workLog.StartTime.HasValue)
                    {
                        var workDuration = workLog.EndTime.Value - workLog.StartTime.Value;

                        if (workLog.LunchBreakStart.HasValue && workLog.LunchBreakEnd.HasValue)
                        {
                            workDuration -= workLog.LunchBreakEnd.Value - workLog.LunchBreakStart.Value;
                        }

                        var standardWorkTime = TimeSpan.FromHours(8);
                        var missingTime = workDuration < standardWorkTime ? standardWorkTime - workDuration : TimeSpan.Zero;
                        var overTime = workDuration > standardWorkTime ? workDuration - standardWorkTime : TimeSpan.Zero;
                    }

                    await dbContext.SaveChangesAsync();
                    MessageBox.Show("Çalışma tamamlandı.", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void WorkConfirm(object sender, RoutedEventArgs e)
        {
            var reportsLogWindow = new ReportsLog(App.ServiceProvider.GetRequiredService<AppDbContext>());
            reportsLogWindow.Show();
        }
    }
}
