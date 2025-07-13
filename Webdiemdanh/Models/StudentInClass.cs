using System.ComponentModel.DataAnnotations;
using Webdiemdanh.Models;

namespace Webdiemdanh.Models
{
    public class StudentInClass
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public int ClassID { get; set; }

        [Required]
        public int StudentID { get; set; }

        public Class Class { get; set; }
        public User Student { get; set; }
    }
}
