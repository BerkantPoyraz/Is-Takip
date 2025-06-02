using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using WorkTracking.DAL.Data;


namespace WorkTrackingWpf
{
    public partial class AdminPanel : Window
    {
        private readonly AppDbContext _context;

        public AdminPanel()
        {
            InitializeComponent();
            _context = new AppDbContext(new DbContextOptions<AppDbContext>());
        }

        private void AddUserLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ContentArea.Content = new AddUserPage(); 
        }

        private void CreateCompanyLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ContentArea.Content = new CreateCompaniesPage();
        }
        private void CreateProjectLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CreateProject createProjectPage = new CreateProject(_context);

            ContentArea.Content = createProjectPage;
        }
        private void ViewReportsLink_Click(object sender, MouseButtonEventArgs e)
        {
            AdminUserReport adminReportsLog = new AdminUserReport(_context);
            adminReportsLog.Show();
        }
    }
}
