namespace WorkTracking.Model.Model
{
    public class Company
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public string CompanyTitle { get; set; }
        public string TaxOffice { get; set; }
        public string TaxNumber { get; set; }
        public string Address { get; set; }

        public List<Project> Projects { get; set; }
    }
}
