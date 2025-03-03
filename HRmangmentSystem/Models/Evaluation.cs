using System;
using System.Collections.Generic;

namespace HRmangmentSystem.Models;

public partial class Evaluation
{
    public int Id { get; set; }

    public DateOnly? DateEvaluated { get; set; }

    public string? Score { get; set; }

    public int? EmployeeId { get; set; }

    public virtual Employee? Employee { get; set; }
}
