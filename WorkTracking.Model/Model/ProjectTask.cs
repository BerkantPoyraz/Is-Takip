namespace WorkTracking.Model.Model
{
    public class ProjectTask
    {
        public int ProjectTaskId { get; set; }
        public string TaskName { get; set; }
        public int ProjectId { get; set; }

        public Project Project { get; set; }
        public List<WorkLog> WorkLogs { get; set; }
    }
}
