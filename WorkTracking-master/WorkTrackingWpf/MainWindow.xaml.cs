using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkTracking.DAL.Repositories;
using WorkTracking.Model.Model;
using System.Security.Cryptography;
using System.Windows.Input;

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
            this.Topmost = true;
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
                    Password = "Admin123+",
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
                App.CurrentUser = user;

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

        private void UserNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click_1(sender, e);
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click_1(sender, e);
            }
        }
    }
}
