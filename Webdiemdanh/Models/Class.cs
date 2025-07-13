using System.ComponentModel.DataAnnotations;
using Webdiemdanh.Models;

namespace Webdiemdanh.Models
{
    public class Class
    {
        [Key]
        public int ClassID { get; set; }

        [Required]
        public string ClassName { get; set; }

        [Required]
        public int TeacherID { get; set; }

        public User Teacher { get; set; }

        public ICollection<StudentInClass>? Students { get; set; }
        public ICollection<Session>? Sessions { get; set; }
    }
}
