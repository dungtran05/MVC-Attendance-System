namespace Webdiemdanh.Data
{
    public class AttendanceDto
    {
        public int SessionId { get; set; }
        public string ClassName { get; set; }  // không bắt buộc
        public string CheckTime { get; set; }  // ISO string
    }
}
