using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webdiemdanh.Data;

namespace Webdiemdanh.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;

        public StudentController(AppDbContext context)
        {
            _context = context;
        }
        public IActionResult Dashboard()
        {
            return View();
        }
        public async Task<IActionResult> Profile()
        {
            int studentId = int.Parse(User.FindFirst("UserID").Value);
            var student = await _context.Users
                .Include(u => u.StudentClasses).ThenInclude(sc => sc.Class)
                .FirstOrDefaultAsync(u => u.UserID == studentId);

            if (student == null) return NotFound();

            return View(student);
        }
    }
}
