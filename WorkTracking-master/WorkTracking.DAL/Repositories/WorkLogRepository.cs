using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; 
using WorkTracking.DAL.Data;
using WorkTracking.Model.Model;


namespace WorkTracking.DAL.Repositories
{
    public class WorkLogRepository : BaseRepository<WorkLog>
    {
        public WorkLogRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<WorkLog>> GetWorkDurationsByUserIdAsync(int userId)
        {
            return await _context.WorkLog
                .Where(w => w.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<WorkLog>> GetWorkDurationsByProjectIdAsync(int projectId)
        {
            return await _context.WorkLog
                .Where(w => w.ProjectId == projectId)
                .ToListAsync();
        }

        public async Task<IEnumerable<WorkLog>> GetWorkDurationsByDateAsync(DateTime date)
        {
            return await _context.WorkLog
                .Where(w => w.StartTime.HasValue && w.StartTime.Value.Date == date.Date)
                .ToListAsync();
        }
    }
}
