using HRmangmentSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Org.BouncyCastle.Crypto;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.StyledXmlParser.Jsoup.Select;

namespace HRmangmentSystem.Controllers
{
    public class ManagerController : Controller
    {

        private readonly MyDbContext _context;
        private readonly EmailService _emailService;

        public ManagerController(MyDbContext context, EmailService emailService)
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
            var Manager = _context.Managers.FirstOrDefault(m => m.Email == Email);
            if (Manager != null && BCrypt.Net.BCrypt.Verify(Password, Manager.Password))
            {

                HttpContext.Session.SetInt32("ManagerId", Manager.Id);
                if (Manager.ProfileImage != null)
                {
                    HttpContext.Session.SetString("ManagerImg", Manager.ProfileImage);

                }
                HttpContext.Session.SetString("ManagerName", Manager.FirstName + " " + Manager.LastName);
                return RedirectToAction("Index");

            }
            else
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

        }


        public async Task<IActionResult> Dashboard()
        {

            ViewBag.TotalEmployees = await _context.Employees.CountAsync();
            ViewBag.TotalLeaveRequests = await _context.Attendances.CountAsync();
            ViewBag.TotalFeedbacks = await _context.EmployeeTasks.CountAsync();


            ViewBag.RecentFeedbacks = await _context.Employees
                .OrderByDescending(f => f.FirstName)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentLeaveRequests = await _context.Attendances
                .Include(l => l.Employee)
                .OrderByDescending(l => l.Date)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentEvaluations = await _context.EmployeeTasks
                .Include(e => e.Employee)
                .OrderByDescending(e => e.StartDate)
                .Take(5)
                .ToListAsync();

            return View();
        }


       
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }


        public IActionResult NewPassword()
        {
            return View();
        }
        [HttpPost]
        [Route("Manager/NewPassword/{id}")]
        public IActionResult NewPassword(string password, int id)
        {
            if (password != null)
            {
                var manager = _context.Managers.Find(id);
                manager.Password = BCrypt.Net.BCrypt.HashPassword(password);
                _context.Managers.Update(manager);
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
            var manger = _context.Managers.FirstOrDefault(e => e.Email == Email);
            await _emailService.SendEmailAsync(Email, "Set New Password", $"https://localhost:7149/Manager/NewPassword/{manger.Id}");
            return View();
        }








        //////////////////////START-MANAGER//////////////////////
        public IActionResult Profile()
        {
            int? managerId = HttpContext.Session.GetInt32("ManagerId");

            if (managerId == null)
            {
                return RedirectToAction("Login");
            }

            var manager = _context.Managers.Find(managerId);
            var department = _context.Departments.Find(manager.DepartmentId);

            ViewBag.DepartmentName = department.DepartmentName;
            return View(manager);
        }




        public IActionResult EditProfile()
        {
            int? managerId = HttpContext.Session.GetInt32("ManagerId");
            var manager = _context.Managers.Find(managerId);
            return View(manager);
        }

        [HttpPost]
        public IActionResult EditProfile(Manager updatedManager, IFormFile ImageFile)
        {
            int? managerId = HttpContext.Session.GetInt32("ManagerId");
            var manager = _context.Managers.Find(managerId);

            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/Manger", uniqueFileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(manager.ProfileImage))
                {
                    string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/Manger", manager.ProfileImage);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                manager.ProfileImage = uniqueFileName;
            }


            manager.FirstName = updatedManager.FirstName;
            manager.LastName = updatedManager.LastName;
            manager.Email = updatedManager.Email;

            _context.Managers.Update(manager);
            _context.SaveChanges();
            HttpContext.Session.SetString("ManagerImg", manager.ProfileImage);
            HttpContext.Session.SetString("ManagerName", manager.FirstName + " " + manager.LastName);

            return RedirectToAction("Profile");
        }




        [HttpPost]
        public IActionResult ResetPassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            int? managerId = HttpContext.Session.GetInt32("ManagerId");
            var manager = _context.Managers.Find(managerId);

            if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, manager.Password))
            {
                TempData["ErrorMessage"] = "Current password is incorrect!";
                return RedirectToAction("Profile");
            }


            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirm password do not match!";
                return RedirectToAction("Profile");
            }

            manager.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            _context.Managers.Update(manager);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Password successfully changed!";
            return RedirectToAction("Profile");
        }


      
        /// ///////////////
       

        public async Task<IActionResult> Index()
        {
            int? managerId = HttpContext.Session.GetInt32("ManagerId");
            if (managerId == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.TotalEmployees = await _context.Employees
                .Where(e => e.ManagerId == managerId)
                .CountAsync();

            ViewBag.TotalLeaveRequests = await _context.LeaveOrVacations
                .Include(l => l.Employee)
                .Where(l => l.Employee.ManagerId == managerId)
                .CountAsync();

            ViewBag.TotalTasks = await _context.EmployeeTasks
                .Include(t => t.Employee)
                .Where(t => t.Employee.ManagerId == managerId)
                .CountAsync();

            ViewBag.RecentLeaveRequests = await _context.LeaveOrVacations
                .Include(l => l.Employee)
                .Where(l => l.Employee.ManagerId == managerId)
                .OrderByDescending(l => l.StartDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentEvaluations = await _context.Evaluations
                .Include(e => e.Employee)
                .Where(e => e.Employee.ManagerId == managerId)
                .OrderByDescending(e => e.DateEvaluated)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentTasks = await _context.EmployeeTasks
                .Include(t => t.Employee)
                .Where(t => t.Employee.ManagerId == managerId)
                .OrderByDescending(t => t.StartDate)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentAttendances = await _context.Attendances
               .Include(a => a.Employee)
               .Where(a => a.Employee.ManagerId == managerId)
               .OrderByDescending(a => a.Date)
               .Take(5)
               .ToListAsync();



            return View();

        }
        public IActionResult AddTask(int id)
        {
            ViewBag.ManagerId = id;
            ViewBag.Employees = new SelectList(_context.Employees.Where(Employees => Employees.ManagerId == id).ToList(), "Id", "FirstName");
            return View();
        }


        public IActionResult AddEvaluation(int id)
        {
            ViewBag.ManagerId = id;
            ViewBag.Employees = new SelectList(_context.Employees.Where(Evaluation => Evaluation.ManagerId == id).ToList(), "Id", "FirstName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(EmployeeTask task)
        {
            task.Id = 0;
            task.Status = "To do";
            var Emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == task.EmployeeId);
            var empEmail = Emp.Email;
            if (ModelState.IsValid)
            {
                _context.Add(task);
                await _context.SaveChangesAsync();
                await _emailService.SendEmailAsync(empEmail, "New task add", $"<h2>Hello {Emp.FirstName}</h2>" +
                    $"<p>A new task has been assigned to you.<br>" +
                    $"<b>task title: </b>{task.Title}<br>" +
                    $"<b>task description: </b>{task.Description}<br>" +
$"<b>Start date: </b>{task.StartDate}<br>" +
$"<b>Due date: </b>{task.DueDate}<br>" +
$"<b>Status: </b>{task.Status}<br>" +


                    $"</p>");

                return RedirectToAction(nameof(ViewEmp), new {id = HttpContext.Session.GetInt32("ManagerId") });
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "Id", task.EmployeeId);
            return View(task);
        }


        public async Task<IActionResult> Vacation(int id)
        {
            var vacations = await _context.LeaveOrVacations
                            .Where(lv => lv.Employee.ManagerId == id)
                            .Include(lv => lv.Employee) // Include employee details if needed
                            .ToListAsync();
            return View(vacations);
        }


        public async Task<IActionResult> UpdateVacation(int id, string approve)
        {
            var vacation = await _context.LeaveOrVacations
                        .Include(v => v.Employee) // Ensure Employee data is included
                        .FirstOrDefaultAsync(v => v.Id == id);
            ;
            if (vacation == null)
            {
                return NotFound();
            }
            vacation.Status = approve;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Vacation), new { id = vacation.Employee.ManagerId });
        }




        public IActionResult ExportToPDF(string reportType, int id)
        {
            List<object> data = new List<object>();
            string title = "";

            switch (reportType)
            {
                case "leaves":
                    data = _context.LeaveOrVacations
                        .Include(l => l.Employee)
                        .Where(l => l.Employee.ManagerId == id)
                        .Select(l => new
                        {
                            Employee = l.Employee.FirstName + " " + l.Employee.LastName,
                            LeaveType = l.LeaveType,
                            StartDate = l.StartDate.HasValue ? l.StartDate.Value.ToShortDateString() : "N/A",
                            EndDate = l.EndDate.HasValue ? l.EndDate.Value.ToShortDateString() : "N/A",
                            Status = l.Status,
                            Reason = l.Reason
                        })
                        .ToList<object>();
                    title = "Leave Requests";
                    break;

                case "employees":
                    data = _context.Employees
                        .Where(e => e.ManagerId == id)
                        .Select(e => new
                        {
                            Name = e.FirstName + " " + e.LastName,
                            Email = e.Email,

                            Manager = e.Manager != null
                                    ? _context.Managers
                                        .Where(m => m.Id == e.ManagerId)
                                        .Select(m => m.FirstName + " " + m.LastName)
                                        .FirstOrDefault() : "No Manager",

                            Department = e.Department != null
                                        ? e.Department.DepartmentName
                                        : _context.Departments
                                        .Where(d => d.Id == e.DepartmentId)
                                        .Select(d => d.DepartmentName).FirstOrDefault() ?? "No Department"
                        })

                        .ToList<object>();
                    title = "Employee List";
                    break;


                case "Attendenc":
                    data = _context.Attendances
                        .Where(a => a.EmployeeId == id)
                        .Select(a => new
                        {
                            Employee = a.Employee.FirstName + " " + a.Employee.LastName,
                            PunchIn = a.PunchIn,
                            PunchOut = a.PunchOut,
                            Hours = a.Hours
                        })
                        .ToList<object>();
                    title = "Attendenc Report";
                    break;

                case "Task":
                    data = _context.EmployeeTasks
                        .Where(a => a.EmployeeId == id)
                        .Select(a => new
                        {
                            Employee = a.Employee.FirstName + " " + a.Employee.LastName,
                            Title = a.Title,
                            Description = a.Description,
                            DueDate = a.DueDate,
                            Status = a.Status
                        })
                        .ToList<object>();
                    title = "Task Report";
                    break;

                default:
                    return BadRequest("Invalid report type");
            }

            using (MemoryStream memoryStream = new MemoryStream()) //إنشاء الذاكرة لتخزين الـ PDF
            {
                PdfWriter writer = new PdfWriter(memoryStream); //PdfWriter هو المسؤول عن كتابة محتوى PDF إلى الذاكرة باستخدام memoryStream
                                                                //نشاء مستند PDF
                PdfDocument pdf = new PdfDocument(writer); //هو الكائن الذي يدير مستند الـ PDF،
                Document document = new Document(pdf);

                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                document.Add(new Paragraph(title).SetFontSize(18));

                document.Add(new Paragraph("\n")); // Add spacing

                // Create Table dynamically
                if (data.Count > 0)
                {
                    var properties = data.First().GetType().GetProperties();
                    Table table = new Table(properties.Length).UseAllAvailableWidth();

                    // Add Headers
                    foreach (var prop in properties)
                    {
                        table.AddHeaderCell(
                            new Cell()
                                .Add(new Paragraph(prop.Name).SetFont(boldFont).SetFontSize(12))
                                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        );
                    }

                    // Add Data
                    foreach (var item in data)
                    {
                        foreach (var prop in properties)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(prop.GetValue(item)?.ToString() ?? "N/A")));
                        }
                    }

                    document.Add(table);
                }

                document.Close();
                return File(memoryStream.ToArray(), "application/pdf", $"{title.Replace(" ", "_")}.pdf");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvaluation(Evaluation evaluation)
        {
            evaluation.Id = 0;
            if (ModelState.IsValid)
            {
                _context.Add(evaluation);
                await _context.SaveChangesAsync();
                return RedirectToAction("EmployeeEvaluation", new { id = evaluation.Id, employeeId = evaluation.EmployeeId });
            }
            ViewData["EmployeeId"] = new SelectList(_context.Employees, "Id", "Id", evaluation.EmployeeId);

            return RedirectToAction("EmployeeEvaluation", new { id = evaluation.Id, employeeId = evaluation.EmployeeId });
        }

        public IActionResult EmployeeEvaluation(int id, int employeeId)
        {
            ViewBag.EvaluationId = id;
            ViewBag.EmployeeId = employeeId;
            return View();
        }



        [HttpPost]
        public IActionResult SubmitEvaluation(int evaluationId, int employeeId, List<int> Scores)
        {
            if (Scores == null || Scores.Count == 0)
            {
                ViewBag.Result = "No scores submitted.";
                return View("EmployeeEvaluation", Scores);
            }

            int totalScore = Scores.Sum();
            double percentage = (totalScore / (Scores.Count * 5.0)) * 100;
            string result;

            if (percentage >= 90)
                result = "Excellent";
            else if (percentage >= 80)
                result = "Very Good";
            else if (percentage >= 70)
                result = "Good";
            else if (percentage >= 60)
                result = "Fair";
            else
                result = "Poor";

            // ✅ Retrieve the specific evaluation by ID
            var evaluation = _context.Evaluations.Find(evaluationId);
            if (evaluation != null)
            {
                evaluation.Score = result;  // Assuming Score is a field in Evaluation model
                _context.Update(evaluation);
                _context.SaveChanges();
            }
            ViewBag.Result = result;

            return View("EmployeeEvaluation");

        }

        public IActionResult ViewEmp(int id)
        {
            ViewBag.ManagerId = id;
            var data = _context.Employees.Include(e => e.Department).Where(e => e.ManagerId == id).ToList();
            return View(data);
        }


        public IActionResult ViewTask(int id)
        {
            ViewBag.EmployeeId = id;
            var data = _context.EmployeeTasks.Where(t => t.EmployeeId == id).ToList();
            return View(data);
        }

        public IActionResult ViewAttendance(int id)
        {
            ViewBag.EmployeeId = id;
            var data = _context.Attendances.Where(a => a.EmployeeId == id).ToList();
            return View(data);
        }

        public IActionResult AddEmp(int id)
        {

            var dep = _context.Managers
                .Include(d => d.Department)
                .FirstOrDefault(m => m.Id == id);
            //ViewBag.ManagerID = id;
            ViewBag.Department = dep.Department?.DepartmentName;
            ViewBag.DepartmentID = dep.DepartmentId;
            return View();
        }


        [HttpPost]
        public IActionResult HandleAddEmp(Employee employee, IFormFile ImageFile)
        {
            if (ImageFile != null)
            {
                string fileName = Path.GetExtension(ImageFile.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/Employee", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                employee.ProfileImage = fileName;
            }
            employee.Password = BCrypt.Net.BCrypt.HashPassword(employee.Password);
            _context.Employees.Add(employee);
            _context.SaveChanges();
            return RedirectToAction("ViewEmp", new {id = employee.ManagerId});






        }


    }
}
