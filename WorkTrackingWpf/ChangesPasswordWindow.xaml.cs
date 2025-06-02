using System.Windows;
using System.Windows.Input;
using WorkTracking.DAL.Data;
using WorkTracking.DAL.Repositories;
using WorkTracking.Model.Model;
using System;


namespace WorkTrackingWpf
{
    public partial class ChangesPasswordWindow : Window
    {
        private readonly IUserRepository _userRepository;
        private readonly User _loggedInUser;

        public ChangesPasswordWindow(IUserRepository userRepository, User loggedInUser)
        {
            InitializeComponent();
            _userRepository = userRepository;
            _loggedInUser = loggedInUser;
        }

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string oldPassword = OldPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (_loggedInUser.Password != oldPassword)
            {
                MessageBox.Show("Eski şifre yanlış.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Yeni şifreler eşleşmiyor.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            {
                MessageBox.Show("Şifre en az 4 karakter olmalıdır.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _loggedInUser.Password = newPassword;
                await _userRepository.UpdateUserAsync(_loggedInUser);

                MessageBox.Show("Şifre başarıyla değiştirildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Şifre değiştirilemedi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ChangePassword_Click(sender, e);
            }
        }

    }
}
