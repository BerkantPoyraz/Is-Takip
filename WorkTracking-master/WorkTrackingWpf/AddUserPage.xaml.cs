using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkTracking.DAL.Data;
using WorkTracking.DAL.Repositories;
using WorkTracking.Model.Model;

namespace WorkTrackingWpf
{
    public partial class AddUserPage : Page
    {
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepository;

        public ObservableCollection<User> Users { get; set; }

        public AddUserPage()
        {
            InitializeComponent();

            _userRepository = App.ServiceProvider.GetRequiredService<IUserRepository>();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            _context = new AppDbContext(optionsBuilder.Options);

            LoadUsers();
        }
        private void LoadUsers()
        {
            try
            {
                var users = _context.Users.ToList();
                Users = new ObservableCollection<User>(users);

                UserListView.ItemsSource = Users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kullanıcılar yüklenirken bir hata oluştu: {ex.Message}");
            }
        }
        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newUser = new User
                {
                    FirstName = FirstNameTextBox.Text,
                    LastName = LastNameTextBox.Text,
                    UserName = UserNameTextBox.Text,
                    Password = PasswordBox.Password,
                    Salary = decimal.Parse(SalaryTextBox.Text),
                    HireDate = HireDatePicker.SelectedDate ?? DateTime.Now,
                    Role = RoleComboBox.SelectedIndex == 0 ? UserRole.Admin : UserRole.User
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                Users.Add(newUser);

                MessageBox.Show("Kullanıcı başarıyla eklendi!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kullanıcı eklenirken bir hata oluştu: {ex.Message}");
            }
        }

        private User _selectedUser;

        private void UserListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            _selectedUser = (User)((ListView)sender).SelectedItem;
        
            if (_selectedUser == null)
            {
                e.Handled = true; 
            }
        }

        private void UpdateUserMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser != null)
            {
                var userRepository = App.ServiceProvider.GetRequiredService<IUserRepository>();

                var updateUserPage = new UpdateUserPage(_selectedUser, userRepository);

                updateUserPage.Show();
            }
            else
            {
                MessageBox.Show("Lütfen güncellemek istediğiniz kullanıcıyı seçin.");
            }
        }
        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        private async void DeleteUserMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedUser = UserListView.SelectedItem as User;

            if (selectedUser != null)
            {
                MessageBoxResult result = MessageBox.Show("Kullanıcıyı silmek istediğinizden emin misiniz?", "Kullanıcı Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _userRepository.DeleteAsync(selectedUser.UserId);

                        UserListView.ItemsSource = await GetUsersAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Hata: {ex.Message}", "Silme Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Lütfen silmek istediğiniz kullanıcıyı seçin.", "Seçim Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
