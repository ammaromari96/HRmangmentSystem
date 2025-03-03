using HRmangmentSystem.Models;
using Microsoft.AspNetCore.Mvc;

using System.Threading.Tasks;

namespace HRmangmentSystem.Controllers
{

    


    public class EmployeeController : Controller
    {
        private readonly MyDbContext _context;
        private readonly EmailService _emailService;
      

        public EmployeeController(MyDbContext context , EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
         
        }


        

       

        public IActionResult Index()
        {
            return View();
        }










        public IActionResult Login()
        {
            return View();
        }




        [HttpPost]
        public IActionResult Login(string Email, string Password)
        {

          


            var employee = _context.Employees.FirstOrDefault(e => e.Email == Email );
            if (employee != null && BCrypt.Net.BCrypt.Verify(Password, employee.Password))
            {
               
                HttpContext.Session.SetInt32("EmployeeId", employee.Id);
                HttpContext.Session.SetString("EmployeeImg", employee.ProfileImage);
                HttpContext.Session.SetString("EmployeeName", employee.FirstName + " " + employee.LastName);
                return RedirectToAction("Dashboard");
            }
            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        public IActionResult Dashboard()
        {
            var attendances = _context.Attendances.ToList(); // Ensure this is fetching data
            return View(attendances); // Pass Model to View
        }



        [HttpPost]
        public IActionResult PunchIn()
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");

            var today = DateOnly.FromDateTime(DateTime.Today);//saves the current date

            var existingRecord = _context.Attendances
                .FirstOrDefault(a => a.EmployeeId == employeeId && a.Date == today);

            if (existingRecord == null)
            {
                var newAttendance = new Attendance
                {
                    Date = today,
                    PunchIn = TimeOnly.FromDateTime(DateTime.Now),
                    Status = "Present",
                    EmployeeId = employeeId
                };

                _context.Attendances.Add(newAttendance);
                _context.SaveChanges();
            }

            return RedirectToAction("Dashboard");
        }




        [HttpPost]
        public IActionResult PunchOut()
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId"); // Get Employee ID from session
            if (employeeId == null)
            {
                return RedirectToAction("Login"); // Redirect if session is not set
            }

            var today = DateOnly.FromDateTime(DateTime.Today);

            var attendance = _context.Attendances
                .FirstOrDefault(a => a.EmployeeId == employeeId && a.Date == today);

            if (attendance != null && attendance.PunchOut == null)
            {
                attendance.PunchOut = TimeOnly.FromDateTime(DateTime.Now);

                if (attendance.PunchIn != null)
                {
                    var punchInTimeSpan = attendance.PunchIn.Value.ToTimeSpan();
                    var punchOutTimeSpan = attendance.PunchOut.Value.ToTimeSpan();

                    // Calculate the difference in TimeSpan
                    var totalHours = punchOutTimeSpan - punchInTimeSpan;

                    // Convert TimeSpan to TimeOnly and store it
                    attendance.Hours = TimeOnly.FromTimeSpan(totalHours);
                }

                _context.Attendances.Update(attendance);
                _context.SaveChanges();
            }

            return RedirectToAction("Dashboard");
        }











        public IActionResult Profile()
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (employeeId == null)
            {
                return RedirectToAction("Login");
            }

            var employee = _context.Employees.Find(employeeId);
            var department = _context.Departments.Find(employee.DepartmentId);
            var manager = _context.Managers.Find(employee.ManagerId);

            var latestEvaluation = _context.Evaluations
        .Where(e => e.EmployeeId == employee.Id)
        .OrderByDescending(e => e.DateEvaluated)
        .FirstOrDefault();


            ViewBag.DepartmentName = department?.DepartmentName;
            ViewBag.ManagerName1 = manager?.FirstName;
            ViewBag.ManagerName2 = manager?.LastName;
            ViewBag.LatestScore = latestEvaluation?.Score;
            return View(employee);
        }







        public IActionResult EditProfile()
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");
            var employee = _context.Employees.Find(employeeId);
            return View(employee);
        }

        [HttpPost]
        public IActionResult EditProfile(Employee updatedEmployee, IFormFile ImageFile)
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");

            var employee = _context.Employees.Find(employeeId);

            if (ImageFile != null && ImageFile.Length > 0)
            {
                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/Employee", uniqueFileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(employee.ProfileImage))
                {
                    string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/image/Employee", employee.ProfileImage);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                employee.ProfileImage = uniqueFileName;
            }


            employee.FirstName = updatedEmployee.FirstName;
            employee.LastName = updatedEmployee.LastName;
            employee.Email = updatedEmployee.Email;

            _context.Update(employee);
            _context.SaveChanges();
            HttpContext.Session.SetString("EmployeeImg", employee.ProfileImage);
            HttpContext.Session.SetString("EmployeeName", employee.FirstName + " " + employee.LastName);

            return RedirectToAction("Profile");
        }


        [HttpPost]
        public IActionResult ResetPassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");
            var employee = _context.Employees.Find(employeeId);

            if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, employee.Password))
            {
                TempData["ErrorMessage"] = "Current password is incorrect!";
                return RedirectToAction("Profile");
            }


            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "New password and confirm password do not match!";
                return RedirectToAction("Profile");
            }

            employee.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);    
            _context.Employees.Update(employee);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Password successfully changed!";
            return RedirectToAction("Profile");
        }


        public IActionResult EmployeeTask()
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");
            var tasks = _context.EmployeeTasks.Where(t => t.EmployeeId == employeeId).ToList();
            return View(tasks);
        }

        [HttpPost]
        public IActionResult UpdateTaskStatus(int taskId, string status)
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");

            var task = _context.EmployeeTasks.Find(taskId);

            task.Status = status;
            _context.EmployeeTasks.Update(task);
            _context.SaveChanges();

            return RedirectToAction("EmployeeTask");
        }

        public IActionResult RequestLeave()
        {
            return View();
        }


        [HttpPost]
        public IActionResult RequestLeave(LeaveOrVacation leaveRequest)
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");

            if (leaveRequest.StartDate > leaveRequest.EndDate)
            {
                TempData["ErrorMessage"] = "End Date must be after Start Date!";
                return View(leaveRequest);
            }

            leaveRequest.EmployeeId = employeeId;
            leaveRequest.Status = "Pending";
            leaveRequest.Id = 0;

            _context.LeaveOrVacations.Add(leaveRequest);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Your leave request has been submitted!";
            return RedirectToAction("LeaveRequests");
        }


        public IActionResult LeaveRequests()
        {
            int? employeeId = HttpContext.Session.GetInt32("EmployeeId");

            var leaveRequests = _context.LeaveOrVacations.Where(l => l.EmployeeId == employeeId).ToList();

            return View(leaveRequests);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("ChoseLogin","Home");
        }

        public IActionResult NewPassword()
        {
            return View();
        }
        [HttpPost]
        [Route("Employee/NewPassword/{id}")]
        public IActionResult NewPassword(string password , int id)
        {
            if (password != null)
            {
                var employee = _context.Employees.Find(id);
                employee.Password = BCrypt.Net.BCrypt.HashPassword(password);
                _context.Employees.Update(employee);
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
        public async Task <IActionResult> ForgetPassword(string Email)
        {
            var employee = _context.Employees.FirstOrDefault(e => e.Email == Email);
            await _emailService.SendEmailAsync(Email,"Set New Password", $"https://localhost:7149/Employee/NewPassword/{employee.Id}");
            return View();
        }










    }



}