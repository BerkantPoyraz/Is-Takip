namespace WorkTracking.Model.Model
{
   public class ProjectTask
{
    public int ProjectTaskId { get; set; }
    public int ProjectId { get; set; }
    public int TaskId { get; set; }

    public Project Project { get; set; }
    public List<WorkLog> WorkLogs { get; set; }
    public NewTask Task { get; set; } 
}
}
