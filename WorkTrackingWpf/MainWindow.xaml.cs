using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WorkTracking.DAL.Repositories;
using WorkTracking.Model.Model;

namespace WorkTrackingWpf
{
    public partial class MainWindow : Window
    {
        private readonly IUserRepository _userRepository;

        public MainWindow()
        {
            InitializeComponent();
            _userRepository = App.ServiceProvider.GetRequiredService<IUserRepository>();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void LoginButton_Click_1(object sender, RoutedEventArgs e)
        {
            string username = UserNameTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorMessage.Text = "Kullanıcı adı ve şifre boş bırakılamaz.";
                return;
            }

            // Kullanıcı doğrulama
            var user = await _userRepository.GetUserByCredentialsAsync(username, password);

            if (user != null)
            {
                var projectRepository = App.ServiceProvider.GetRequiredService<IProjectRepository>();
                var userRepository = App.ServiceProvider.GetRequiredService<IUserRepository>();

                // loggedInUser'ı doğru şekilde geçiriyoruz
                ProjectSelectionPage projectPage = new ProjectSelectionPage(projectRepository, userRepository, user);
                projectPage.Show();
                this.Close(); // MainWindow'ı kapatıyoruz
            }
            else
            {
                ErrorMessage.Text = "Geçersiz kullanıcı adı veya şifre.";
            }
        }
    }
}
