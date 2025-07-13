using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Webdiemdanh.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [RegularExpression("Teacher|Student")]
        public string Role { get; set; } // Chỉ nhận "Teacher" hoặc "Student"

        public byte[]? FaceEncoding { get; set; } // vector mã hóa khuôn mặt (không lưu ảnh)

        public ICollection<StudentInClass>? StudentClasses { get; set; }
        public ICollection<Class>? ClassesTaught { get; set; }
        public ICollection<Attendance>? Attendances { get; set; }
    }
}
