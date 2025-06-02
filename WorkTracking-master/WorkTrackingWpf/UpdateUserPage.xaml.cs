using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using WorkTracking.DAL.Repositories;

namespace WorkTrackingWpf
{
    public partial class UpdateUserPage : Window
    {
        private User _user;
        private readonly IUserRepository _userRepository;

        public UpdateUserPage(User user, IUserRepository userRepository)
        {
            _user = user;
            _userRepository = userRepository;
            InitializeComponent();

            FirstNameTextBox.Text = _user.FirstName;
            LastNameTextBox.Text = _user.LastName;
            UserNameTextBox.Text = _user.UserName;
            PasswordBox.Password = _user.Password;
            SalaryTextBox.Text = _user.Salary.ToString("N0");
            HireDatePicker.SelectedDate = _user.HireDate;
            RoleComboBox.SelectedItem = _user.Role == UserRole.Admin ? "Admin" : "Kullanıcı";
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            _user.FirstName = FirstNameTextBox.Text;
            _user.LastName = LastNameTextBox.Text;
            _user.UserName = UserNameTextBox.Text;
            _user.Password = PasswordBox.Password;
            _user.Salary = decimal.Parse(SalaryTextBox.Text);
            _user.HireDate = HireDatePicker.SelectedDate ?? DateTime.Now;
            _user.Role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() == "Admin" ? UserRole.Admin : UserRole.User;

            await _userRepository.UpdateAsync(_user);
            this.Close();
        }
    }
}
