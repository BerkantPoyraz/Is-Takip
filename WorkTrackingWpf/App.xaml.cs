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

            if (!EnsureDatabaseMigrated())
            {
                Shutdown();
            }
        }

        private bool EnsureDatabaseMigrated()
        {
            try
            {
                Console.WriteLine("Veritabanı bağlantısı kontrol ediliyor...");
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    if (dbContext.Database.CanConnect())
                    {
                        Console.WriteLine("Veritabanı bağlantısı sağlandı.");
                        var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();

                        if (pendingMigrations.Any())
                        {
                            Console.WriteLine("Eksik migration'lar var, uygulama işlemi başlatılıyor...");
                            dbContext.Database.Migrate();
                            MessageBox.Show($"Eksik migration'lar uygulandı:\n- {string.Join("\n- ", pendingMigrations)}",
                                "Bilgilendirme", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                           // MessageBox.Show("Veritabanı güncel.", "Bilgilendirme", MessageBoxButton.OK, MessageBoxImage.Information);
                        }

                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Veritabanı bağlantısı sağlanamadı. Lütfen SQL Server'ın çalıştığından ve bağlantı bilgilerinizi kontrol ettiğinizden emin olun.",
                            "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Veritabanı işlemi sırasında hata: {ex.Message}");
                MessageBox.Show($"Veritabanı işlemi sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }

}
