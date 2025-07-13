using System.ComponentModel.DataAnnotations;
using Webdiemdanh.Models;

namespace Webdiemdanh.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceID { get; set; }

        [Required]
        public int SessionID { get; set; }

        [Required]
        public int StudentID { get; set; }

        public bool IsPresent { get; set; }

        public DateTime? CheckedTime { get; set; }

        public bool VerifiedByFace { get; set; } = true;

        public Session Session { get; set; }
        public User Student { get; set; }
    }
}
