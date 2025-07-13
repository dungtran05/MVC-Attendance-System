using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webdiemdanh.Data;
using Webdiemdanh.Models;

namespace Webdiemdanh.Controllers
{
    
    public class AttendanceController : Controller
    {
        private readonly AppDbContext _context;

        public AttendanceController(AppDbContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "Teacher")]
        // GET: /Attendance/Sessions
        public IActionResult Sessions()
        {
            int teacherId = int.Parse(User.FindFirst("UserID").Value);
            var sessions = _context.Sessions
                .Include(s => s.Class)
                .Where(s => s.Class.TeacherID == teacherId)
                .OrderByDescending(s => s.SessionDate)
                .ToList();

            return View(sessions); // Views/Attendance/Sessions.cshtml
        }

        // GET: /Attendance/BySession?sessionId=5
        [Authorize(Roles = "Teacher")]
        public IActionResult BySession(int sessionId)
        {
            var attendance = _context.Attendances
                .Include(a => a.Student)
                .Include(a => a.Session)
                .ThenInclude(s => s.Class)
                .Where(a => a.SessionID == sessionId)
                .ToList();

            return View(attendance); // Views/Attendance/BySession.cshtml
        }
        [Authorize(Roles = "Teacher")]
        public IActionResult CurrentTeacherSessions()
        {
            int teacherId = int.Parse(User.FindFirst("UserID").Value);
            DateTime today = DateTime.Today;
            TimeSpan now = DateTime.Now.TimeOfDay;

            var sessions = _context.Sessions
                .Include(s => s.Class)
                .Include(s => s.Attendances).ThenInclude(a => a.Student)
                .Where(s =>
                    s.Class.TeacherID == teacherId &&
                    s.SessionDate == today &&
                    s.StartTime <= now &&
                    s.EndTime >= now
                )
                .OrderBy(s => s.StartTime)
                .ToList();

            return View("CurrentTeacherSessions", sessions);
        }
        [Authorize(Roles = "Student")]
        public IActionResult CurrentStudentSessions()
        {
            int studentId = int.Parse(User.FindFirst("UserID").Value);
            DateTime today = DateTime.Today;
            TimeSpan now = DateTime.Now.TimeOfDay;

            var sessions = _context.Sessions
                .Include(s => s.Class)
                .ThenInclude(c => c.Teacher)
                .Where(s =>
                    s.SessionDate == today &&
                    s.StartTime <= now &&
                    s.EndTime >= now &&
                    _context.StudentsInClass.Any(sc =>
                        sc.ClassID == s.ClassID && sc.StudentID == studentId
                    )
                )
                .OrderBy(s => s.StartTime)
                .ToList();

            return View("CurrentStudentSessions", sessions);
        }
        [Authorize(Roles = "Student")]
        public IActionResult Mark(int sessionId)
        {
            ViewBag.SessionId = sessionId;
            return View(); // View = Mark.cshtml
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> CheckIn([FromBody] CheckInDto model)
        {
            int studentId = int.Parse(User.FindFirst("UserID").Value);

            // Lấy thông tin buổi học
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.SessionID == model.SessionId);
            if (session == null)
            {
                return NotFound("Không tìm thấy buổi học.");
            }

            // Kiểm tra thời gian điểm danh có nằm trong giờ học không
            DateTime now = DateTime.Now;
            if (session.SessionDate != now.Date || now.TimeOfDay < session.StartTime || now.TimeOfDay > session.EndTime)
            {
                return BadRequest("Bạn chỉ được điểm danh trong thời gian diễn ra buổi học.");
            }

            var existing = await _context.Attendances
                .FirstOrDefaultAsync(a => a.SessionID == model.SessionId && a.StudentID == studentId);

            if (existing != null && existing.IsPresent)
            {
                return BadRequest("Bạn đã điểm danh trước đó.");
            }

            if (existing == null)
            {
                existing = new Attendance
                {
                    SessionID = model.SessionId,
                    StudentID = studentId
                };
                _context.Attendances.Add(existing);
            }   

            existing.IsPresent = true;
            existing.CheckedTime = now;
            existing.VerifiedByFace = model.VerifiedByFace;

            await _context.SaveChangesAsync();

            return Ok("Điểm danh thành công.");
        }

        [Authorize(Roles = "Student")]
        public IActionResult MyHistory()
        {
            int studentId = int.Parse(User.FindFirst("UserID").Value);

            var history = _context.Attendances
                .Include(a => a.Session)
                .ThenInclude(s => s.Class)
                .Where(a => a.StudentID == studentId)
                .OrderByDescending(a => a.Session.SessionDate)
                .ThenByDescending(a => a.Session.StartTime)
                .ToList();

            return View("MyHistory", history);
        }
        // GET: /Attendance/Take?sessionId=4
        [Authorize(Roles = "Teacher")]
        public IActionResult Take(int sessionId)
        {
            var session = _context.Sessions
                .Include(s => s.Class)
                .ThenInclude(c => c.Students) // nếu bạn có navigation property
                .Include(s => s.Attendances)
                .ThenInclude(a => a.Student)
                .FirstOrDefault(s => s.SessionID == sessionId);

            if (session == null) return NotFound();

            // Lấy danh sách sinh viên trong lớp (nếu không có navigation property thì dùng StudentsInClass)
            var studentIds = _context.StudentsInClass
                .Where(sc => sc.ClassID == session.ClassID)
                .Select(sc => sc.StudentID)
                .ToList();

            var allStudents = _context.Users
                .Where(u => studentIds.Contains(u.UserID))
                .ToList();

            // Gộp danh sách sinh viên với Attendance
            foreach (var student in allStudents)
            {
                if (!session.Attendances.Any(a => a.StudentID == student.UserID))
                {
                    session.Attendances.Add(new Attendance
                    {
                        Student = student,
                        StudentID = student.UserID,
                        SessionID = session.SessionID,
                        IsPresent = false
                    });
                }
            }

            return View(session);
        }


        // POST: /Attendance/Take
        [HttpPost]
        [Authorize(Roles = "Teacher")]
        public IActionResult Take(int sessionId, Dictionary<int, bool> attendance)
        {
            // Lấy danh sách toàn bộ sinh viên trong lớp của session đó
            var session = _context.Sessions
                .Include(s => s.Class)
                .FirstOrDefault(s => s.SessionID == sessionId);

            var studentIds = _context.StudentsInClass
                .Where(sc => sc.ClassID == session.ClassID)
                .Select(sc => sc.StudentID)
                .ToList();

            var existingRecords = _context.Attendances
                .Where(a => a.SessionID == sessionId)
                .ToList();

            foreach (var studentId in studentIds)
            {
                bool isPresent = attendance.ContainsKey(studentId) && attendance[studentId];

                var record = existingRecords.FirstOrDefault(a => a.StudentID == studentId);

                if (record != null)
                {
                    record.IsPresent = isPresent;

                    if (!record.VerifiedByFace)
                    {
                        record.CheckedTime = isPresent ? DateTime.Now : null;
                    }
                }
                else
                {
                    _context.Attendances.Add(new Attendance
                    {
                        SessionID = sessionId,
                        StudentID = studentId,
                        IsPresent = isPresent,
                        CheckedTime = isPresent ? DateTime.Now : null,
                        VerifiedByFace = false
                    });
                }
            }

            _context.SaveChanges();
            return RedirectToAction("BySession", new { sessionId });
        }

        [Authorize(Roles = "Teacher")]
        public IActionResult SessionsByClass(int classId)
        {
            var sessions = _context.Sessions
                .Include(s => s.Class)
                .Where(s => s.ClassID == classId)
                .OrderByDescending(s => s.SessionDate)
                .ThenByDescending(s => s.StartTime)
                .ToList();

            ViewBag.ClassId = classId;
            ViewBag.ClassName = sessions.FirstOrDefault()?.Class?.ClassName ?? "Lớp chưa rõ";

            return View("SessionsByClass", sessions);
        }




    }
}
