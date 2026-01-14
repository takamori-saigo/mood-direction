using System.Security.Claims;
using Aplication.Services;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoralCompass.Infrastructure.Domain;
using MoralCompass.Infrastructure.Domain.Enums;

namespace Aplication.Controllers
{
    [Authorize]
    public class IndexController : Controller
    {
        private readonly MainPageService _pageService;
        private readonly MoralCompassDbContext _context;
        
        public IndexController(MoralCompassDbContext context, MainPageService  mainPageService)
        {
            _pageService = mainPageService;
            _context = context;
        }

        public class HomeIndexModel
        {
            public List<CoreThesis> Theses { get; set; } = new();
            public List<DiscussionItem> TopDilemmas { get; set; } = new();
        }
        
        public IActionResult Stats()
        {
            return View();
        }
    
        public class UserProfileModel
        {
            public string Email { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Nickname { get; set; }
            public string? Gender { get; set; } 
            public int? Age { get; set; }
        }
        
        public async Task<IActionResult> Index()
        {
            var theses = await _pageService.GetAllThesesByActive();

            // var topDilemmaIds = await _pageService.GetTopDilemmsIdAsync();
    
            // var topDilemmas = await _pageService.GetTopDilemmasById(topDilemmaIds);
    
            var allDilemmas = await _context.DiscussionItems
                .Include(di => di.Author) // обязательно, чтобы отображался Nickname
                .ToListAsync();

            var random = new Random();
            var randomDilemmas = allDilemmas
                .OrderBy(_ => random.Next())
                .Take(7)
                .ToList();

    
            var model = new HomeIndexModel { Theses = theses, TopDilemmas = randomDilemmas };

            return View(model);
        }
        
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            // Получаем строку GUID из claims
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(); // Неверный или отсутствующий ID
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            var model = new UserProfileModel
            {
                Email = user.Email,
                Phone = user.Phone,
                Nickname = user.Nickname,
                Gender = user.Gender.ToString(),
                Age = user.Age
            };

            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.Nickname = model.Nickname ?? user.Nickname;
            user.Phone = model.Phone;

            // 🔑 Преобразуем строку в enum
            if (!string.IsNullOrWhiteSpace(model.Gender))
            {
                if (Enum.TryParse<Gender>(model.Gender, true, out var gender))
                {
                    user.Gender = gender;
                }
                // Если не удалось распарсить — оставляем старое значение
            }

            user.Age = model.Age;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Профиль успешно обновлён!";
            return RedirectToAction(nameof(Profile));
        }
    }
}
