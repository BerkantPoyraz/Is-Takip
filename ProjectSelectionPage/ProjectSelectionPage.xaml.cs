using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkTracking.DAL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using ProjectSelectionPage;

namespace WorkTrackingWpf
{
    public partial class ProjectSelectionPage : Window
    {
        private readonly IProjectRepository _projectRepository;

        // ProjectSelectionPage sınıfı, dependency injection ile _projectRepository'yi alır
        public ProjectSelectionPage()
        {
            InitializeComponent();  // XAML dosyasındaki bileşenleri başlatır

            // ServiceProvider üzerinden projeyi alıyoruz
            _projectRepository = App.ServiceProvider.GetRequiredService<IProjectRepository>();

            // Asenkron verileri UI Thread'inden ayırarak almak için LoadProjectsAsync metodunu çağırıyoruz
            LoadProjectsAsync();
        }

        // Asenkron olarak projeleri yüklemek için bir metod
        private async Task LoadProjectsAsync()
        {
            try
            {
                // Firma ID'si (örneğin 1) ile projeleri alıyoruz
                var projects = await _projectRepository.GetProjectsByFirmIdAsync(1);

                // Verilerin yüklendiğini kontrol et
                if (projects == null || !projects.Any())
                {
                    MessageBox.Show("No projects found.");
                }
                else
                {
                    // Projeleri ListBox'a bind ediyoruz
                    ProjectsListBox.ItemsSource = projects;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading projects: {ex.Message}");
            }
        }
    }
}
