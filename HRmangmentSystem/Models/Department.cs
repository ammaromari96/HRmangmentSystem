﻿using System;
using System.Collections.Generic;

namespace HRmangmentSystem.Models;

public partial class Department
{
    public int Id { get; set; }

    public string? DepartmentName { get; set; }

    public string? Description { get; set; }

    public int? EmployeeCount { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual Manager? Manager { get; set; }
}
