using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkTracking.DAL.Repositories;
using WorkTracking.Model.Model;
using System.Security.Cryptography;

namespace WorkTrackingWpf
{
    public partial class MainWindow : Window
    {
        private readonly IUserRepository _userRepository;

        public MainWindow()
        {
            InitializeComponent();
            _userRepository = App.ServiceProvider.GetRequiredService<IUserRepository>();
            SeedAdminUser();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        private async Task SeedAdminUser()
        {
            var adminUser = await _userRepository.GetUserByUserNameAsync("Admin");
            if (adminUser == null)
            {
                var newAdmin = new User
                {
                    UserName = "Admin",
                    FirstName = "Admin",
                    LastName = "Admin",
                    Password = "Admin_123456+", 
                    Role = UserRole.Admin,
                    Salary = 0,
                    HireDate = DateTime.Now
                };

                await _userRepository.AddAsync(newAdmin);
            }
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

            var user = await _userRepository.GetUserByCredentialsAsync(username, password);

            if (user != null)
            {
                var projectRepository = App.ServiceProvider.GetRequiredService<IProjectRepository>();
                var userRepository = App.ServiceProvider.GetRequiredService<IUserRepository>();

                ProjectSelectionPage projectPage = new ProjectSelectionPage(projectRepository, userRepository, user);
                projectPage.Show();
                this.Close();
            }
            else
            {
                ErrorMessage.Text = "Geçersiz kullanıcı adı veya şifre.";
            }
        }
    }
}
