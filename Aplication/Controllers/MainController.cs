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
        private readonly MoralCompassDbContext _context;

        public class HomeIndexModel
        {
            public List<CoreThesis> Theses { get; set; } = new();
            public List<DiscussionItem> TopDilemmas { get; set; } = new();
        }
        
        public IndexController(MoralCompassDbContext context)
        {
            _context = context;
        }
        
        public IActionResult Stats()
        {
            return View();
        }
    
        public async Task<IActionResult> Index()
        {
            if (!await _context.CoreTheses.AnyAsync())
            {
                var seedTheses = new List<CoreThesis>
                {
                    new() { Title = "Чти жизнь", Description = "Жизнь — главная ценность. Не посягай на чужую и защищай свою.", Order = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Уважай чужое", Description = "Не бери чужого, не нарушай личные границы.", Order = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Говори правду", Description = "Ложь разрушает доверие. Говори честно, но с добротой.", Order = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Держи слово", Description = "Обещание — долг. Выполняй, что обещал.", Order = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Помогай слабым", Description = "Сила — в защите тех, кто не может защитить себя.", Order = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Развивайся", Description = "Стремись к знаниям, добру и мудрости — каждый день.", Order = 6, IsActive = true, CreatedAt = DateTime.UtcNow }
                };

                await _context.CoreTheses.AddRangeAsync(seedTheses);
                await _context.SaveChangesAsync();
            }

            // 🔸 Загружаем данные для отображения
            var theses = await _context.CoreTheses
                .Where(ct => ct.IsActive)
                .OrderBy(ct => ct.Order)
                .Take(6)
                .ToListAsync();

            var topDilemmaIds = await _context.Comments
                .Where(c => c.DiscussionItem != null)
                .GroupBy(c => c.DiscussionItemId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.Id)
                .Take(3)
                .Select(x => x.Id)
                .ToListAsync();

            var topDilemmas = await _context.DiscussionItems
                .Where(di => di.Type == DiscussionItemType.Dilemma && topDilemmaIds.Contains(di.Id))
                .ToListAsync();

            topDilemmas = topDilemmaIds
                .Select(id => topDilemmas.FirstOrDefault(di => di.Id == id))
                .Where(di => di != null)
                .ToList();

            var model = new HomeIndexModel
            {
                Theses = theses,
                TopDilemmas = topDilemmas
            };

            return View(model);
        }
    }
}