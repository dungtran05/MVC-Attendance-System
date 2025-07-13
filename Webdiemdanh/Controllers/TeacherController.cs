using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Webdiemdanh.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        [Authorize(Roles = "Teacher")]
        public IActionResult Dashboard()
        {
            return View();
        }
    }

}
