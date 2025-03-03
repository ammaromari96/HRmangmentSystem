using HRmangmentSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using HRmangmentSystem.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;
using iTextSharp.text;


namespace HRmangmentSystem.Controllers
{
    public class HrController : Controller
    {
        private readonly MyDbContext _context;
        private readonly EmailService _emailService;

        public HrController(MyDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }



        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]
        public IActionResult Login(string Email, string Password)
        {

            var hr = _context.Hrs.FirstOrDefault(h => h.Email == Email );
            var password1 = Password;
            var Virify = hr.Password;
            if (hr != null && BCrypt.Net.BCrypt.Verify(password1, Virify))
            {
                HttpContext.Session.SetInt32("HrId", hr.Id);
                HttpContext.Session.SetString("HrImg", hr.ProfileImage);
                HttpContext.Session.SetString("HrName", hr.FirstName + " " + hr.LastName);
                return RedirectToAction("Dashboard");
            }
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        public IActionResult NewPassword()
        {
            return View();
        }
        [HttpPost]
        [Route("Hr/NewPassword/{id}")]
        public IActionResult NewPassword(string password, int id)
        {
            if (password != null)
            {
                var hr = _context.Hrs.Find(id);
                hr.Password = BCrypt.Net.BCrypt.HashPassword(password);
                 _context.Hrs.Update(hr);
                _context.SaveChanges();
                return RedirectToAction("Login");
            }


            return View();
        }


        public IActionResult ForgetPassword()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> ForgetPassword(string Email)
        {
            var hr = _context.Hrs.FirstOrDefault(e => e.Email == Email);
            await _emailService.SendEmailAsync(Email, "Set New Password", $"https://localhost:7149/Hr/NewPassword/{hr.Id}");
            return View();
        }



        //////////////////////START-HR//////////////////////
        public IActionResult Profile()
        {
            int? hrId = HttpContext.Session.GetInt32("HrId");

            if (hrId == null)
            {
                return RedirectToAction("Login");
            }

            var hr = _context.Hrs.Find(hrId);

            return View(hr);
        }




        public IActionResult EditProfile()
        {
            int? hrId = HttpContext.Session.GetInt32("HrId");
            var hr = _context.Hrs.Find(hrId);
            return View(hr);
        }

        [HttpPost]
        public IActionResult EditProfile(Hr updatedHr, IFormFile ImageFile)
        {
            int? hrId = HttpContext.Session.GetInt32("HrId");

            var hr = _context.Hrs.Find(hrId);

            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/HR", uniqueFileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(hr.ProfileImage))
                {
                    string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/HR", hr.ProfileImage);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                hr.ProfileImage = uniqueFileName;
            }


            hr.FirstName = updatedHr.FirstName;
            hr.LastName = updatedHr.LastName;
            hr.Email = updatedHr.Email;

            _context.Hrs.Update(hr);
            _context.SaveChanges();
            HttpContext.Session.SetString("HrImg", hr.ProfileImage);
            HttpContext.Session.SetString("HrName", hr.FirstName + " " + hr.LastName);

            return RedirectToAction("Profile", "HR");
        }




        [HttpPost]
        public IActionResult ResetPassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            int? hrId = HttpContext.Session.GetInt32("HrId");
            var hr = _context.Hrs.Find(hrId);

            if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, hr.Password))
            {
                TempData["ErrorMessage"] = "Current password is incorrect!";
                return RedirectToAction("Profile");
            }


            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirm password do not match!";
                return RedirectToAction("Profile");
            }

            hr.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            
            _context.Hrs.Update(hr);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Password successfully changed!";
            return RedirectToAction("Profile");
        }


        //////////////////////END-HR//////////////////////


        public async Task<IActionResult> Dashboard()
        {

            ViewBag.TotalEmployees = await _context.Employees.CountAsync();
            ViewBag.TotalDepartments = await _context.Departments.CountAsync();
            ViewBag.TotalManagers = await _context.Managers.CountAsync();
            ViewBag.TotalLeaveRequests = await _context.LeaveOrVacations.CountAsync();
            ViewBag.TotalFeedbacks = await _context.Feedbacks.CountAsync();
            ViewBag.TotalEvaluations = await _context.Evaluations.CountAsync();


            ViewBag.RecentFeedbacks = await _context.Feedbacks
                .OrderByDescending(f => f.SubmittedDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentLeaveRequests = await _context.LeaveOrVacations
                .Include(l => l.Employee)
                .OrderByDescending(l => l.StartDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentEvaluations = await _context.Evaluations
                .Include(e => e.Employee)
                .OrderByDescending(e => e.DateEvaluated)
                .Take(5)
                .ToListAsync();

            return View();
        }




        public IActionResult Departments()
        {
            var departments = _context.Departments
                .Include(d => d.Manager)
                .ToList();
            return View(departments);
        }



        public IActionResult AddDepartment()
        {
            ViewBag.DepartmentName = "";
            ViewBag.Description = "";
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddDepartment(string departmentName, string description, int? employeeCount)
        {
            if (string.IsNullOrEmpty(departmentName))
            {
                ModelState.AddModelError("", "Department name is required");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var department = new Department
                    {
                        DepartmentName = departmentName,
                        Description = description,
                        EmployeeCount = employeeCount ?? 0
                    };

                    _context.Departments.Add(department);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Department created successfully!";
                    return RedirectToAction("Departments");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error: {ex.Message}");
                }
            }

            ViewBag.DepartmentName = departmentName;
            ViewBag.Description = description;
            ViewBag.EmployeeCount = employeeCount;

            return View();
        }


        public IActionResult ViewManagers()
        {
            var managers = _context.Managers
                .Include(m => m.Department)
                .ToList();

            return View(managers);
        }




        public IActionResult ViewEmployees()
        {
            var employees = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Manager)
                .ToList();

            return View(employees);
        }



        public IActionResult Managers()
        {
            var managers = _context.Managers.ToList();
            return View(managers);
        }


        public async Task<IActionResult> LeaveRequests()
        {
            var leaveRequests = await _context.LeaveOrVacations
                .Include(l => l.Employee)
                .ThenInclude(e => e.Department)
                .Where(l => l.Status == "Approved")
                .ToListAsync();
            return View(leaveRequests);
        }



        public async Task<IActionResult> Feedbacks()
        {
            var feedbacks = await _context.Feedbacks
                .OrderByDescending(f => f.SubmittedDate)
                .ToListAsync();
            return View(feedbacks);
        }


        public async Task<IActionResult> Evaluations()
        {
            var evaluations = await _context.Evaluations
                .Include(e => e.Employee)
                .ThenInclude(emp => emp.Department)
                .OrderByDescending(e => e.DateEvaluated)
                .ToListAsync();
            return View(evaluations);
        }




        public IActionResult DownloadLeaveRequestsPDF()
        {
            var leaveRequests = _context.LeaveOrVacations.Include(l => l.Employee).ToList();

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4);
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(0, 0, 0));
                Paragraph title = new Paragraph("Leave Requests", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                pdfDoc.Add(title);

                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3f, 3f, 3f, 3f });

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(255, 255, 255));
                BaseColor headerColor = new BaseColor(114, 103, 239);
                PdfPCell[] headers = {
                new PdfPCell(new Phrase("Employee", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Leave Type", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Start Date", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Status", headerFont)) { BackgroundColor = headerColor } };

                foreach (var header in headers)
                {
                    header.HorizontalAlignment = Element.ALIGN_CENTER;
                    header.Padding = 5;
                    table.AddCell(header);
                }

                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, new BaseColor(0, 0, 0));
                foreach (var leave in leaveRequests)
                {
                    table.AddCell(new PdfPCell(new Phrase($"{leave.Employee.FirstName} {leave.Employee.LastName}", cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(leave.LeaveType, cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(leave.StartDate?.ToString("yyyy-MM-dd") ?? "N/A", cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(leave.Status, cellFont)) { Padding = 5 });
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", "LeaveRequests.pdf");
            }
        }




        public IActionResult DownloadFeedbacksPDF()
        {
            var feedbacks = _context.Feedbacks.ToList();

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4);
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(0, 0, 0));
                Paragraph title = new Paragraph("Feedbacks", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                pdfDoc.Add(title);

                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3f, 3f, 5f });

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(255, 255, 255));
                BaseColor headerColor = new BaseColor(114, 103, 239);
                PdfPCell[] headers = {
                new PdfPCell(new Phrase("Name", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Email", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Message", headerFont)) { BackgroundColor = headerColor } };

                foreach (var header in headers)
                {
                    header.HorizontalAlignment = Element.ALIGN_CENTER;
                    header.Padding = 5;
                    table.AddCell(header);
                }

                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, new BaseColor(0, 0, 0));
                foreach (var feedback in feedbacks)
                {
                    table.AddCell(new PdfPCell(new Phrase(feedback.Name, cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(feedback.Email, cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(feedback.Message, cellFont)) { Padding = 5 });
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", "Feedbacks.pdf");
            }
        }


        public IActionResult DownloadManagersPDF()
        {
            var managers = _context.Managers.Include(m => m.Department).ToList();

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4);
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(0, 0, 0));
                Paragraph title = new Paragraph("Managers List", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                pdfDoc.Add(title);

                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3f, 3f, 3f });

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(255, 255, 255));
                BaseColor headerColor = new BaseColor(114, 103, 239);
                PdfPCell[] headers = {
                new PdfPCell(new Phrase("Name", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Email", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Department", headerFont)) { BackgroundColor = headerColor }};

                foreach (var header in headers)
                {
                    header.HorizontalAlignment = Element.ALIGN_CENTER;
                    header.Padding = 5;
                    table.AddCell(header);
                }

                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, new BaseColor(0, 0, 0));
                foreach (var manager in managers)
                {
                    table.AddCell(new PdfPCell(new Phrase($"{manager.FirstName} {manager.LastName}", cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(manager.Email, cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(manager.Department?.DepartmentName ?? "N/A", cellFont)) { Padding = 5 });
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", "Managers.pdf");
            }
        }


        public IActionResult DownloadEmployeesPDF()
        {
            var employees = _context.Employees.Include(e => e.Department).Include(e => e.Manager).ToList();

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4);
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(0, 0, 0));
                Paragraph title = new Paragraph("Employees List", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                pdfDoc.Add(title);

                PdfPTable table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3f, 3f, 3f, 3f });

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(255, 255, 255));
                BaseColor headerColor = new BaseColor(114, 103, 239);
                PdfPCell[] headers = {
                new PdfPCell(new Phrase("Name", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Email", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Department", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Manager", headerFont)) { BackgroundColor = headerColor }};

                foreach (var header in headers)
                {
                    header.HorizontalAlignment = Element.ALIGN_CENTER;
                    header.Padding = 5;
                    table.AddCell(header);
                }

                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, new BaseColor(0, 0, 0));
                foreach (var employee in employees)
                {
                    table.AddCell(new PdfPCell(new Phrase($"{employee.FirstName} {employee.LastName}", cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(employee.Email, cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(employee.Department?.DepartmentName ?? "N/A", cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(employee.Manager != null ? $"{employee.Manager.FirstName} {employee.Manager.LastName}" : "No Manager", cellFont)) { Padding = 5 });
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", "Employees.pdf");
            }
        }


        public IActionResult DownloadDepartmentsPDF()
        {
            var departments = _context.Departments.Include(d => d.Manager).ToList();

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4);
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(0, 0, 0));
                Paragraph title = new Paragraph("Departments List", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                pdfDoc.Add(title);

                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 3f, 5f, 3f });

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(255, 255, 255));
                BaseColor headerColor = new BaseColor(114, 103, 239);
                PdfPCell[] headers = {
                new PdfPCell(new Phrase("Department Name", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Description", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Manager", headerFont)) { BackgroundColor = headerColor } };

                foreach (var header in headers)
                {
                    header.HorizontalAlignment = Element.ALIGN_CENTER;
                    header.Padding = 5;
                    table.AddCell(header);
                }


                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, new BaseColor(0, 0, 0));
                foreach (var department in departments)
                {
                    table.AddCell(new PdfPCell(new Phrase(department.DepartmentName, cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(department.Description ?? "N/A", cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(department.Manager != null ? $"{department.Manager.FirstName} {department.Manager.LastName}" : "No Manager", cellFont)) { Padding = 5 });
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", "Departments.pdf");
            }
        }


        public IActionResult DownloadEvaluationsPDF()
        {
            var evaluations = _context.Evaluations.Include(e => e.Employee).ToList();

            using (MemoryStream stream = new MemoryStream())
            {
                Document pdfDoc = new Document(PageSize.A4);
                PdfWriter.GetInstance(pdfDoc, stream);
                pdfDoc.Open();

                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(0, 0, 0));
                Paragraph title = new Paragraph("Employee Evaluations", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                pdfDoc.Add(title);

                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 4f, 2f, 3f });

                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, new BaseColor(255, 255, 255));
                BaseColor headerColor = new BaseColor(114, 103, 239);
                PdfPCell[] headers = {
                new PdfPCell(new Phrase("Employee Name", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Score", headerFont)) { BackgroundColor = headerColor },
                new PdfPCell(new Phrase("Date Evaluated", headerFont)) { BackgroundColor = headerColor } };

                foreach (var header in headers)
                {
                    header.HorizontalAlignment = Element.ALIGN_CENTER;
                    header.Padding = 5;
                    table.AddCell(header);
                }

                // Table Data Styling
                var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 11, new BaseColor(0, 0, 0));
                foreach (var evaluation in evaluations)
                {
                    table.AddCell(new PdfPCell(new Phrase($"{evaluation.Employee.FirstName} {evaluation.Employee.LastName}", cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(evaluation.Score, cellFont)) { Padding = 5 });
                    table.AddCell(new PdfPCell(new Phrase(evaluation.DateEvaluated?.ToString("yyyy-MM-dd") ?? "N/A", cellFont)) { Padding = 5 });
                }

                pdfDoc.Add(table);
                pdfDoc.Close();

                return File(stream.ToArray(), "application/pdf", "Evaluations.pdf");
            }
        }




        [HttpGet]
        public async Task<IActionResult> AddManager()
        {
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddManager(Manager manager)
        {
            if (ModelState.IsValid)
            {
                if (_context.Managers.Any(m => m.Email == manager.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    ViewBag.Departments = await _context.Departments.ToListAsync();
                    return View(manager);
                }

                if (manager.DepartmentId == 0)
                {
                    ModelState.AddModelError("DepartmentID", "Please select a department.");
                    ViewBag.Departments = await _context.Departments.ToListAsync();
                    return View(manager);
                }

                manager.Password = BCrypt.Net.BCrypt.HashPassword(manager.Password);
                manager.Id = 0;
                _context.Managers.Add(manager);
                await _context.SaveChangesAsync();
                return RedirectToAction("ViewManagers");
            }

            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View();
        }



    }
}
