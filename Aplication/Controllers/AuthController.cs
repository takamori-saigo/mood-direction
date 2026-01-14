using System.Security.Claims;
using Aplication.Services;
using Domain.DTO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aplication.Controllers;

public class AuthController : Controller 
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Index");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Index");
        
        if (!ModelState.IsValid) return View(model);

        try
        {
            var user = await _authService.LoginAsync(model);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Nickname),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("Age", user.Age.ToString()),
                new Claim("Gender", user.Gender.ToString()),
                new Claim("IsAdmin", user.IsAdmin.ToString()) // ← ДОБАВЬТЕ ЗДЕСЬ
            };

            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync( CookieAuthenticationDefaults.AuthenticationScheme,
                principal, new AuthenticationProperties
                {
                    IssuedUtc = DateTimeOffset.UtcNow,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                    IsPersistent = true
                });

            return RedirectToAction("Index", "Index"); 
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }
    
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Index");
        
        if (!ModelState.IsValid) return View(model);
        
        try
        {
            await _authService.RegisterAsync(model);
            return RedirectToAction("Index", "Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }
    
    [HttpGet]
    public IActionResult ExternalLogin(string provider)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth");

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };

        return Challenge(properties, provider);
    }


    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback()
    {
        var result = await HttpContext.AuthenticateAsync();

        if (!result.Succeeded || result.Principal == null)
            return RedirectToAction("Login");

        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? "User";

        
        if (email == null)
            return RedirectToAction("Login");

        var user = await _authService.GetOrCreateExternalUserAsync(email, name);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Nickname),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("IsAdmin", user.IsAdmin.ToString()) 
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        
        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IssuedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
                IsPersistent = true
            });
        
        return RedirectToAction("Index", "Index");
    }
    
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Index");
    }
}