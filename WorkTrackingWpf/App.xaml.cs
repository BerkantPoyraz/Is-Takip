using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using WorkTracking.DAL.Data;
using WorkTracking.DAL.Repositories;

namespace WorkTrackingWpf
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static User CurrentUser { get; set; }
        private System.Timers.Timer _connectionCheckTimer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configuration setup
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IConfiguration>(configuration);

            // Register DbContext
            serviceCollection.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")),
                ServiceLifetime.Transient);

            // Register repositories
            serviceCollection.AddScoped<IProjectRepository, ProjectRepository>();
            serviceCollection.AddScoped<IUserRepository, UserRepository>();
            serviceCollection.AddScoped<ICompanyRepository, CompanyRepository>();

            // Register main window and other views
            serviceCollection.AddSingleton<MainWindow>();
            serviceCollection.AddTransient<ReportsLog>();

            // Build the service provider
            ServiceProvider = serviceCollection.BuildServiceProvider();

            EnsureDatabaseCreated();

            StartConnectionCheckTimer();
        }

        private void EnsureDatabaseCreated()
        {
            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    dbContext.Database.EnsureCreated();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanı oluşturulurken bir hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void StartConnectionCheckTimer()
        {
            _connectionCheckTimer = new System.Timers.Timer
            {
                Interval = TimeSpan.FromHours(1).TotalMilliseconds
            };

            _connectionCheckTimer.Elapsed += async (sender, args) =>
            {
                await CheckDatabaseConnection();
            };

            _connectionCheckTimer.Start();
        }

        private async System.Threading.Tasks.Task CheckDatabaseConnection()
        {
            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    if (!await dbContext.Database.CanConnectAsync())
                    {
                        throw new Exception("Veritabanı bağlantısı kesildi.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanı bağlantısı kontrol edilirken bir hata oluştu: {ex.Message}",
                    "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
