namespace WorkTracking.Model.Model
{
    public class WorkLog
    {
        public int WorkLogId { get; set; }
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public int? ProjectTaskId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }  

        public User User { get; set; }
        public Project Project { get; set; }
        public ProjectTask ProjectTask { get; set; }
    }
}
