using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoralCompass.Infrastructure.Domain;
using MoralCompass.Infrastructure.Domain.Enums;

namespace Aplication.Controllers
{
    public class MainController : Controller
    {
        private readonly MoralCompassDbContext _context;

        public class HomeIndexModel
        {
            public List<CoreThesis> Theses { get; set; } = new();
            public List<DiscussionItem> TopDilemmas { get; set; } = new();
        }
        
        public MainController(MoralCompassDbContext context)
        {
            _context = context;
        }
    
        public async Task<IActionResult> Index()
        {
            var theses = await _context.CoreTheses
                .Where(ct => ct.IsActive)
                .OrderBy(ct => ct.Order)
                .Take(6)
                .ToListAsync();

            // 2. ТОП-3 дилеммы по сумме реакций
            var topDilemmaIds = await _context.Reactions
                .Where(r => r.TargetType == ReactionTargetType.DiscussionItem)
                .GroupBy(r => r.TargetId)
                .Select(g => new { Id = g.Key, Score = g.Sum(x => x.Value) })
                .OrderByDescending(x => x.Score)
                .Take(3)
                .Select(x => x.Id)
                .ToListAsync();

            var topDilemmas = await _context.DiscussionItems
                .Where(di => di.Type == DiscussionItemType.Dilemma && topDilemmaIds.Contains(di.Id))
                .ToListAsync();

            // Сортируем в памяти по порядку из topDilemmaIds (чтобы сохранить рейтинг)
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

