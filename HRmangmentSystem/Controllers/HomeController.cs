using System.Diagnostics;
using HRmangmentSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRmangmentSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly MyDbContext _context;

       public HomeController(ILogger<HomeController> logger, MyDbContext context)
        {
            _logger = logger;
            _context = context;
        }


      

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }


        public IActionResult ContactUS()
        {
            return View();
        }

        public async Task<IActionResult> Department()
        {
            var deps = await _context.Departments.ToListAsync();
            return View(deps);
        }

        [HttpPost]
        public IActionResult SubmitFeedback(string name, string email, string message)
        {
            if (ModelState.IsValid)
            {
                Feedback feedback = new Feedback
                {
                    Name = name ?? "",  // Ensure it's not null
                    Email = email ?? "",
                    Message = message ?? "",
                    SubmittedDate = DateOnly.FromDateTime(DateTime.Today) // Correct conversion
                };


                _context.Feedbacks.Add(feedback);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Your feedback has been submitted successfully!";
                return RedirectToAction("ContactUS");
            }

            TempData["ErrorMessage"] = "There was an error submitting your feedback. Please try again.";
            return View("ContactUS");
        }




        public IActionResult ChoseLogin()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
