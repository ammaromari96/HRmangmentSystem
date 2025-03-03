using System;
using System.Collections.Generic;

namespace HRmangmentSystem.Models;

public partial class Employee
{
    public int Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? ProfileImage { get; set; }

    public int? DepartmentId { get; set; }

    public int? ManagerId { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual Department? Department { get; set; }

    public virtual ICollection<EmployeeTask> EmployeeTasks { get; set; } = new List<EmployeeTask>();

    public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();

    public virtual ICollection<LeaveOrVacation> LeaveOrVacations { get; set; } = new List<LeaveOrVacation>();

    public virtual Manager? Manager { get; set; }
}
