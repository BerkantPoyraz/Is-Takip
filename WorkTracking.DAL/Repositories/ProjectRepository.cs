using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;

namespace WorkTracking.DAL.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _context;

        public ProjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            return await _context.Projects.ToListAsync();
        }

        public async Task<List<ProjectTask>> GetProjectTasksByProjectIdAsync(int projectId)
        {
            return await _context.ProjectTasks.Where(t => t.ProjectId == projectId).ToListAsync();
        }
        public Project GetProjectById(int projectId)
        {
            return _context.Projects.FirstOrDefault(p => p.ProjectId == projectId);
        }
    }
}
