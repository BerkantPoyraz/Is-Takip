using WorkTracking.Model.Model;

public class NewTask
{
    public int TaskId { get; set; }
    public string TaskName { get; set; }

    public List<ProjectTask> ProjectTasks { get; set; }
}