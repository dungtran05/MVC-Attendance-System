using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Webdiemdanh.Data;
using Webdiemdanh.Models;

namespace Webdiemdanh.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaceAttendanceController : ControllerBase
    {
        public class YoloCheckInDto
        {
            public string ImageBase64 { get; set; }
        }

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _context;

        public FaceAttendanceController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("yolo-checkin")]
        public async Task<IActionResult> YoloCheckIn([FromQuery] int sessionId, [FromBody] YoloCheckInDto dto)
        {
            // Decode base64 thành byte[]
            var imageBytes = Convert.FromBase64String(dto.ImageBase64.Split(',')[1]);
            var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(imageBytes), "image", "snapshot.jpg");

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync("http://localhost:5000/predict", content);

            if (!response.IsSuccessStatusCode)
                return StatusCode(500, "Không gọi được API YOLO");

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonDocument.Parse(json);
            var predictions = data.RootElement.GetProperty("predictions");

            if (predictions.GetArrayLength() == 0)
                return Ok(new { message = "Không nhận diện được học sinh." });

            var best = predictions[0];
            string className = best.GetProperty("class_name").GetString();
            double confidence = best.GetProperty("confidence").GetDouble();

            // Ví dụ: dựa vào tên class, ta tra bảng user
            var student = _context.Users.FirstOrDefault(u => u.FullName == className && u.Role == "Student");
            if (student == null)
                return Ok(new { message = $"Không tìm thấy học sinh tên {className} trong hệ thống." });

            // Ghi điểm danh nếu chưa có
            var exist = _context.Attendances
                .FirstOrDefault(a => a.SessionID == sessionId && a.StudentID == student.UserID);

            if (exist == null)
            {
                exist = new Attendance
                {
                    SessionID = sessionId,
                    StudentID = student.UserID,
                    IsPresent = true,
                    CheckedTime = DateTime.Now,
                    VerifiedByFace = true
                };
                _context.Attendances.Add(exist);
            }
            else if (!exist.IsPresent)
            {
                exist.IsPresent = true;
                exist.CheckedTime = DateTime.Now;
                exist.VerifiedByFace = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Điểm danh thành công: {student.FullName} ({confidence * 100:F1}%)" });
        }
    }
}
