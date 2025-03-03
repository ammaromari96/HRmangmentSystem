using System;
using System.Collections.Generic;

namespace HRmangmentSystem.Models;

public partial class EmployeeTask
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public int? EmployeeId { get; set; }

    public virtual Employee? Employee { get; set; }
}
