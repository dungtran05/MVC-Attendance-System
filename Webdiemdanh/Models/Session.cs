using System.ComponentModel.DataAnnotations;
using Webdiemdanh.Models;

namespace Webdiemdanh.Models
{
    public class Session
    {
        [Key]
        public int SessionID { get; set; }

        [Required]
        public int ClassID { get; set; }

        [Required]
        public DateTime SessionDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public Class Class { get; set; }

        public ICollection<Attendance>? Attendances { get; set; }
    }
}
