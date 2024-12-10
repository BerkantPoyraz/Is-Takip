using WorkTracking.Model.Model;

public enum UserRole
{
    Admin = 1,
    User = 0
}

public class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }

    public UserRole Role { get; set; }

    public List<WorkLog> WorkLogs { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public string SalaryFormatted => Salary.ToString("N0");

}
