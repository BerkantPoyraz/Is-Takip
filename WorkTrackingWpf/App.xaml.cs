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

            // Register repositories
            serviceCollection.AddScoped<IProjectRepository, ProjectRepository>();
            serviceCollection.AddScoped<IUserRepository, UserRepository>();
            serviceCollection.AddScoped<ICompanyRepository, CompanyRepository>();

            serviceCollection.AddSingleton<MainWindow>();
            serviceCollection.AddTransient<ReportsLog>();

            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

    }
}
