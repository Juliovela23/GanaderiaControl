using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace GanaderiaControl.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult Error()
        {
            // Puedes leer más info de HttpContext.Features si quieres registrar
            Response.StatusCode = 500;
            return View();
        }

        [AllowAnonymous]
        public IActionResult StatusCode(int code)
        {
            // 404, 403, etc.
            Response.StatusCode = code;
            ViewBag.Code = code;
            return View();
        }
    }
}
