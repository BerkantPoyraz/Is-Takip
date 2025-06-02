using System.Collections.Generic;
using System.Threading.Tasks;
using WorkTracking.Model.Model;

namespace WorkTracking.DAL.Repositories
{
    public interface IProjectRepository
    {
        Task<List<Project>> GetProjectsAsync();
        Task<List<ProjectTask>> GetProjectTasksByProjectIdAsync(int projectId);
    }

}
