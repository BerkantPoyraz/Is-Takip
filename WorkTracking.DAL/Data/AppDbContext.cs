using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WorkTracking.Model.Model;

namespace WorkTracking.DAL.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                var connectionString = configuration.GetConnectionString("DefaultConnection");

                try
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Veritabanına bağlanılamadı. Lütfen bağlantı ayarlarını kontrol edin.", ex);
                }

                optionsBuilder.UseSqlServer(connectionString);
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Company>()
        .HasKey(c => c.CompanyId);

    modelBuilder.Entity<Project>()
        .HasKey(p => p.ProjectId);

    modelBuilder.Entity<Project>()
        .HasOne(p => p.ContractType)
        .WithMany() 
        .HasForeignKey(p => p.ContractTypeId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<Project>()
        .HasMany(p => p.ProjectTasks)
        .WithOne(t => t.Project)
        .HasForeignKey(t => t.ProjectId);

    modelBuilder.Entity<WorkLog>()
        .HasOne(w => w.Project)
        .WithMany(p => p.WorkLogs)
        .HasForeignKey(w => w.ProjectId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<WorkLog>()
        .HasOne(w => w.User)
        .WithMany(u => u.WorkLogs)
        .HasForeignKey(w => w.UserId);

    modelBuilder.Entity<WorkLog>()
        .HasOne(w => w.ProjectTask)
        .WithMany(t => t.WorkLogs)
        .HasForeignKey(w => w.ProjectTaskId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<ProjectTask>()
        .HasOne(pt => pt.Task)
        .WithMany(nt => nt.ProjectTasks)
        .HasForeignKey(pt => pt.TaskId)
        .OnDelete(DeleteBehavior.Restrict);

    modelBuilder.Entity<NewTask>()
        .HasKey(C => C.TaskId);

    modelBuilder.Entity<ContractType>()
        .HasKey(c => c.ContractTypeId);

    modelBuilder.Entity<UserReports>()
        .HasKey(ur => ur.UserReportsId);

    modelBuilder.Entity<UserReports>()
        .HasOne(ur => ur.User)
        .WithMany()
        .HasForeignKey(ur => ur.UserId);

    // Property conversion for UserReports Status
    modelBuilder.Entity<UserReports>()
        .Property(u => u.Status)
        .HasConversion<int>();

    // TaskWithSelection doesn't need a key
    modelBuilder.Entity<TaskWithSelection>().HasNoKey();

    base.OnModelCreating(modelBuilder);
}


        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<WorkLog> WorkLog { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<ContractType> ContractTypes { get; set; }
        public DbSet<NewTask> Tasks { get; set; }
        public DbSet<TaskWithSelection> TaskWithSelections { get; set; }
        public DbSet<UserReports> UserReports { get; set; }
    }
}