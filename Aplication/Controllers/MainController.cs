using Microsoft.AspNetCore.Mvc;

namespace Aplication.Controllers;

public class MainController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
