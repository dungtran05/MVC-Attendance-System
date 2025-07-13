using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webdiemdanh.Data;
using Webdiemdanh.Models;

namespace Webdiemdanh.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class ClassController : Controller
    {
        private readonly AppDbContext _context;

        public ClassController(AppDbContext context)
        {
            _context = context;
        }

        // Trang tạo lớp học
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(Class model)
        {
            int teacherId = int.Parse(User.FindFirst("UserID").Value);
            model.TeacherID = teacherId;

            // ❗ Kiểm tra xem tên lớp đã tồn tại chưa
            bool isExist = await _context.Classes.AnyAsync(c => c.ClassName == model.ClassName);
            if (isExist)
            {
                ModelState.AddModelError("ClassName", "❌ Tên lớp học đã tồn tại. Vui lòng chọn tên khác.");

                // Load danh sách sinh viên để render lại form
                ViewBag.AllStudents = await _context.Users
                    .Where(u => u.Role == "Student")
                    .ToListAsync();

                return View(model);
            }

            _context.Classes.Add(model);
            await _context.SaveChangesAsync();

            // Load lại thông tin lớp + sinh viên
            var createdClass = await _context.Classes
                .Include(c => c.Teacher)
                .Include(c => c.Students)
                .ThenInclude(sc => sc.Student)
                .FirstOrDefaultAsync(c => c.ClassID == model.ClassID);

            ViewBag.AllStudents = await _context.Users
                .Where(u => u.Role == "Student")
                .ToListAsync();

            return View(createdClass);
        }



        // Danh sách các lớp giáo viên đã tạo
        public IActionResult MyClasses()
        {
            int teacherId = int.Parse(User.FindFirst("UserID").Value);
            var classes = _context.Classes
                .Include(c => c.Teacher) // <- THÊM DÒNG NÀY
                .Where(c => c.TeacherID == teacherId)
                .ToList();

            return View(classes);
        }


        // Quản lý lớp: xem sinh viên trong lớp
        public async Task<IActionResult> Manage(int id)
        {
            var cls = await _context.Classes
                .Include(c => c.Students) // Students = ICollection<StudentInClass>
                    .ThenInclude(sc => sc.Student) // <-- dùng Student chứ không phải User
                .FirstOrDefaultAsync(c => c.ClassID == id);

            if (cls == null) return NotFound();

            // Lấy tất cả sinh viên (User có Role = Student)
            ViewBag.AllStudents = await _context.Users
                .Where(u => u.Role == "Student")
                .ToListAsync();

            return View(cls);
        }

        // Thêm một sinh viên vào lớp
        [HttpPost]
        public async Task<IActionResult> AddStudent(int classId, int studentId)
        {
            bool exists = await _context.StudentsInClass
                .AnyAsync(s => s.ClassID == classId && s.StudentID == studentId);

            if (!exists)
            {
                _context.StudentsInClass.Add(new StudentInClass
                {
                    ClassID = classId,
                    StudentID = studentId
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Manage", new { id = classId });
        }

        // Trang chọn nhiều sinh viên để thêm vào lớp
        public IActionResult AddStudents(int classId)
        {
            var students = _context.Users
                .Where(u => u.Role == "Student" &&
                            !_context.StudentsInClass
                                .Any(sc => sc.StudentID == u.UserID && sc.ClassID == classId))
                .ToList();

            ViewBag.ClassId = classId;
            return View(students);
        }

        // Xử lý thêm nhiều sinh viên
        [HttpPost]
        public IActionResult AddStudentsToClass(int classId, List<int> selectedStudentIds)
        {
            foreach (var studentId in selectedStudentIds)
            {
                _context.StudentsInClass.Add(new StudentInClass
                {
                    ClassID = classId,
                    StudentID = studentId
                });
            }

            _context.SaveChanges();
            return RedirectToAction("Manage", new { id = classId });
        }

        // (Tuỳ chọn) Xóa sinh viên khỏi lớp
        [HttpPost]
        public async Task<IActionResult> RemoveStudent(int classId, int studentId)
        {
            var entry = await _context.StudentsInClass
                .FirstOrDefaultAsync(s => s.ClassID == classId && s.StudentID == studentId);

            if (entry != null)
            {
                _context.StudentsInClass.Remove(entry);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Manage", new { id = classId });
        }
        public async Task<IActionResult> Edit(int id)
        {
            var cls = await _context.Classes.FindAsync(id);
            if (cls == null) return NotFound();
            return View(cls);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Class updatedClass)
        {
            _context.Classes.Update(updatedClass);
            await _context.SaveChangesAsync();
            return RedirectToAction("MyClasses");
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var cls = await _context.Classes.FindAsync(id);
            if (cls != null)
            {
                _context.Classes.Remove(cls);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("MyClasses");
        }
        [HttpPost]
        public async Task<IActionResult> AddStudentsToClassDefault(int classId, List<int> selectedStudentIds)
        {
            foreach (var studentId in selectedStudentIds)
            {
                bool alreadyInClass = await _context.StudentsInClass
                    .AnyAsync(sc => sc.ClassID == classId && sc.StudentID == studentId);

                if (!alreadyInClass)
                {
                    _context.StudentsInClass.Add(new StudentInClass
                    {
                        ClassID = classId,
                        StudentID = studentId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Manage", new { id = classId });
        }

    }
}
