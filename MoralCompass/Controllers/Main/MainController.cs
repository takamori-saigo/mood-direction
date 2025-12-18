using Microsoft.AspNetCore.Mvc;

namespace MoralCompass.Controllers.Main;

public class MainController: Controller
{
    public IActionResult Main() => View();
}