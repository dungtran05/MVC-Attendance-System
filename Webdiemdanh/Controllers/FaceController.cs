using Microsoft.AspNetCore.Mvc;

namespace Webdiemdanh.Controllers
{
    public class FaceController : Controller
    {
        public IActionResult RegisterFace()
        {
            return View(); // Views/Face/RegisterFace.cshtml
        }
    }
}
