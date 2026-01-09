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
        
        public IndexController(MoralCompassDbContext context)
        {
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
    
        public async Task<IActionResult> Index()
        {
            if (!await _context.CoreTheses.AnyAsync())
            {
                var seedTheses = new List<CoreThesis>
                {
                    new() { Title = "Чти жизнь", Description = "Жизнь — главная ценность. Не посягай на чужую и защищай свою.", Order = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Уважай чужое", Description = "Нельзя присваивать силой или обманом то, что тебе не принадлежит.", Order = 2, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Не причиняй вреда", Description = "Ты свободен делать что угодно, пока твои слова и действия не ущемляют других.", Order = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Помогай ближнему", Description = "Не усугубляй страдания. Протяни руку, если есть возможность.", Order = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Будь честен", Description = "Не лги, не оговаривай, не притворяйся, не лицемерь, не вводи в заблуждение.", Order = 5, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new() { Title = "Держи слово", Description = "Отвечай за обещания. Договорённости - это ответственность. Репутация - основа доверия.", Order = 6, IsActive = true, CreatedAt = DateTime.UtcNow }
                };

                await _context.CoreTheses.AddRangeAsync(seedTheses);
                await _context.SaveChangesAsync();
            }

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

            List<DiscussionItem?> topDilemmas = await _context.DiscussionItems
                .Where(di => di.Type == DiscussionItemType.Dilemma && topDilemmaIds.Contains(di.Id))
                .Include(di => di.Author) 
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
