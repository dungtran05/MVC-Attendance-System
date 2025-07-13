using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Webdiemdanh.Data;
using Webdiemdanh.Models;

namespace Webdiemdanh.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class SessionController : Controller
    {
        private readonly AppDbContext _context;

        public SessionController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {
            int teacherId = int.Parse(User.FindFirst("UserID").Value);
            ViewBag.Classes = new SelectList(
                _context.Classes.Where(c => c.TeacherID == teacherId),
                "ClassID", "ClassName"
            );
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Session model)
        {
            _context.Sessions.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("MySessions");
        }

        public IActionResult MySessions()
        {
            int teacherId = int.Parse(User.FindFirst("UserID").Value);

            var sessions = _context.Sessions
                .Include(s => s.Class)
                .ThenInclude(s=>s.Teacher)
                .Where(s => s.Class.TeacherID == teacherId)
                .OrderByDescending(s => s.SessionDate)
                .ThenByDescending(s => s.StartTime)
                .ToList();

            return View("MySessions", sessions);
        }
        [Authorize(Roles = "Teacher")]
        public IActionResult Edit(int id)
        {
            var session = _context.Sessions.FirstOrDefault(s => s.SessionID == id);
            if (session == null) return NotFound();

            return View(session);
        }
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Edit(Session session)
        {
            if (ModelState.IsValid)
            {
                _context.Sessions.Update(session);
                await _context.SaveChangesAsync();
                return RedirectToAction("MySessions", new { classId = session.ClassID });
            }

            return View(session);
        }
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _context.Sessions.FindAsync(id);
            if (session != null)
            {
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("MySessions", new { classId = session.ClassID });
        }


    }

}
