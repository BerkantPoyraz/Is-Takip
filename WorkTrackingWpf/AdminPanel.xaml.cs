using System.Windows;
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
            _context = new AppDbContext(new DbContextOptions<AppDbContext>()); // You should pass the proper options for AppDbContext
        }

        // Kullanıcı Ekle Linki Tıklandığında
        private void AddUserLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Kullanıcı ekleme sayfasını ContentArea içine yükleme
            ContentArea.Content = new AddUserPage(); // CreateUser sayfasını göstereceğiz.
        }

        // Firma Ekle Linki Tıklandığında
        private void CreateCompanyLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Firma ekleme sayfasını ContentArea içine yükleme
            ContentArea.Content = new CreateCompaniesPage(); // CreateCompany sayfasını göstereceğiz.
        }

        private void CreateProjectLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Pass _context to CreateProject constructor
            CreateProject createProjectPage = new CreateProject(_context);

            ContentArea.Content = createProjectPage;
        }
    }
}
