using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoralCompass.Data;
using MoralCompass.Models;

namespace MoralCompass.Controllers.Auth;

public class AuthorizationController: Controller
{
    private readonly ApplicationDbContext _context;

    public AuthorizationController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public IActionResult Authorization() => View();

    [HttpPost]
    public async Task<IActionResult> Authorization(AuthorizationModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        
        var loginExist = await _context.Users.AnyAsync(u => u.Login == model.Login);
        var passwordExist = await _context.Users.AnyAsync(u => u.PasswordHash == HashPassword.GetHashPassword(model.Password));

        if (loginExist && passwordExist)
        {
            return RedirectToAction("Main", "Main");
        }
        
        return View(model);
    }
}