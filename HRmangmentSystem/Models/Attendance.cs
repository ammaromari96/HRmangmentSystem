using System;
using System.Collections.Generic;

namespace HRmangmentSystem.Models;

public partial class Attendance
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public TimeOnly? PunchIn { get; set; }

    public TimeOnly? PunchOut { get; set; }

    public TimeOnly? Hours { get; set; }

    public string? Status { get; set; }

    public int? EmployeeId { get; set; }

    public virtual Employee? Employee { get; set; }
}
