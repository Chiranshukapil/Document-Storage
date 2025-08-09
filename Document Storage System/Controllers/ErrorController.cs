// File: Controllers/ErrorController.cs
using Microsoft.AspNetCore.Mvc;

namespace Document_Storage_System.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/404")]
        public IActionResult Error404()
        {
            return View("Error404"); // Ensure Views/Error/Error404.cshtml exists
        }

        [Route("Error/{code}")]
        public IActionResult GeneralError(int code)
        {
            if (code == 404)
                return RedirectToAction("Error404");
            return View("Error"); // Fallback for 500, 403, etc.
        }
    }
}
