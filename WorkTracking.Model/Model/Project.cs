namespace WorkTracking.Model.Model
{
    public enum ContractCurrency
    {
        TL = 0,
        USD = 1,
        EUR = 2
    }
    public enum ProjectStatus
    {
        Pasif = 0,
        Aktif = 1
    }
    public class Project
    {
        public int ProjectId { get; set; }
        public string ProjectNumber { get; set; }
        public string ProjectName { get; set; }
        public DateTime ProjectStartTime { get; set; }
        public DateTime? ProjectEndTime { get; set; }
        public int CompanyId { get; set; }
        public int ContractTypeId { get; set; }
        public decimal ContractAmount { get; set; }
        public decimal UnitAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public ContractCurrency ContractCurrency { get; set; }
        public ProjectStatus ProjectStatus { get; set; }

        public ContractType ContractType { get; set; }

        public List<ProjectTask> ProjectTasks { get; set; }
        public List<WorkLog> WorkLogs { get; set; }
        public Company Company { get; set; }
    }
}
