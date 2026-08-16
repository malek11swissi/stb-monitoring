namespace HrSimulator.Models;

public sealed class Employee
{
    public string Matricule { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string EmploymentStatus { get; set; } = "ACTIVE";
    public DateTime HiredAt { get; set; }
}
