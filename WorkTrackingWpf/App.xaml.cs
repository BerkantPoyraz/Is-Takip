using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WorkTracking.DAL.Data;
using WorkTracking.DAL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

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
                        // Veritabanı yoksa oluştur
                        dbContext.Database.EnsureCreated();
                        MessageBox.Show("Veritabanı bulunamadı. Yeni bir veritabanı oluşturuldu.",
                            "Bilgilendirme", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
                    if (pendingMigrations.Any())
                    {
                        dbContext.Database.Migrate();
                        MessageBox.Show($"Eksik migration'lar uygulandı:\n- {string.Join("\n- ", pendingMigrations)}",
                            "Bilgilendirme", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
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
