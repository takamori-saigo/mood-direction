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
                .ThenByDescending(x => x.Id) // стабильность: новее — выше при равных
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