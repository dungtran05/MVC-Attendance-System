using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization; // Thêm dòng này
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Webdiemdanh.Data;
using Webdiemdanh.Models;

public class AuthController : Controller
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous] // ✅ Cho phép truy cập không cần đăng nhập
    public IActionResult Login() => View();

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string email, string password)
    {
        string hashedPassword = HashPassword(password);

        var user = _context.Users
            .FirstOrDefault(u => u.Email == email && u.PasswordHash == hashedPassword);

        if (user == null)
        {
            ViewBag.Error = "Email hoặc mật khẩu không đúng";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("UserID", user.UserID.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false // ✅ Không giữ đăng nhập sau khi đóng trình duyệt
            });

        return user.Role == "Teacher"
            ? RedirectToAction("Dashboard", "Teacher")
            : RedirectToAction("Dashboard", "Student");
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View();

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(string fullName, string email, string password, string role)
    {
        if (_context.Users.Any(u => u.Email == email))
        {
            ViewBag.Error = "Email đã được sử dụng!";
            return View();
        }

        if (role != "Teacher" && role != "Student")
        {
            ViewBag.Error = "Vai trò không hợp lệ.";
            return View();
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = HashPassword(password),
            Role = role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        ViewBag.Message = "Đăng ký thành công. Bạn có thể đăng nhập.";
        return View();
    }
}
