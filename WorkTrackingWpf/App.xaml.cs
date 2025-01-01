using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;
using WorkTracking.DAL.Data;
using WorkTracking.DAL.Repositories;

namespace WorkTrackingWpf
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static User CurrentUser { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IConfiguration>(configuration);

            serviceCollection.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")),
                ServiceLifetime.Transient);

            serviceCollection.AddScoped<IProjectRepository, ProjectRepository>();
            serviceCollection.AddScoped<IUserRepository, UserRepository>();
            serviceCollection.AddScoped<ICompanyRepository, CompanyRepository>();

            serviceCollection.AddSingleton<MainWindow>();
            serviceCollection.AddTransient<ReportsLog>();

            ServiceProvider = serviceCollection.BuildServiceProvider();

            EnsureDatabaseMigrated();
        }
        private void EnsureDatabaseMigrated()
        {
            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    if (!dbContext.Database.CanConnect())
                    {
                        dbContext.Database.EnsureCreated();
                        MessageBox.Show("Veritabanı bulunamadı. Yeni bir veritabanı oluşturuldu.",
                            "Bilgilendirme", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

                        if (pendingMigrations.Any())
                        {
                            dbContext.Database.Migrate();
                            MessageBox.Show("Veritabanı migration işlemi başarılı bir şekilde tamamlandı.",
                                "Bilgilendirme", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            //MessageBox.Show("Veritabanı zaten güncel.",
                            //    "Bilgilendirme", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanı oluşturulurken veya migrate edilirken bir hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
