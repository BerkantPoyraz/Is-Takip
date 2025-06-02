using WorkTracking.Model.Model;

namespace WorkTracking.DAL.Repositories
{
    public interface IWorkLogRepository
    {
        Task<IEnumerable<WorkLog>> GetWorkLogsByUserIdAsync(int userId);
    }
}
