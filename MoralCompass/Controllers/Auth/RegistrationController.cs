using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoralCompass.Models;
using MoralCompass.Data;

namespace MoralCompass.Controllers.Auth;

public class RegistrationController: Controller
{
    private readonly ApplicationDbContext _context;

    
    public RegistrationController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public IActionResult Registration() => View();
    

    [HttpPost]
    public async Task<IActionResult> Registration(RegistrationModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new User
        {
            Email = model.Email,
            Login = model.Login,
            PasswordHash = HashPassword.GetHashPassword(model.Password)
        };
        
        var emailExist = await _context.Users.AnyAsync(u => u.Email == model.Email);
        var loginExist = await _context.Users.AnyAsync(u => u.Login == model.Login);
        
        if (emailExist || loginExist)
            return View(model);
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return RedirectToAction("Registration");
    }
    
}