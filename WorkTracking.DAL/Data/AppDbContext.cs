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
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<WorkLog> WorkLog { get; set; }
        public DbSet<ProjectTask> ProjectTasks { get; set; }
        public DbSet<ContractType> ContractTypes { get; set; }
    }
}
