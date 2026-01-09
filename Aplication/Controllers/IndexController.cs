using Aplication.Services;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoralCompass.Infrastructure.Domain;

namespace Aplication.Controllers
{
    [Authorize]
    public class IndexController : Controller
    {
        private readonly MoralCompassDbContext _context;
        private readonly MainPageService _pageService;
        
        public IndexController(MoralCompassDbContext context, MainPageService  mainPageService)
        {
            _context = context;
            _pageService = mainPageService;
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
            var theses = await _pageService.GetAllThesesByActive();

            var topDilemmaIds = await _pageService.GetTopDilemmsIdAsync();
            
            var topDilemmas = await _pageService.GetTopDilemmasById(topDilemmaIds);

            var model = new HomeIndexModel { Theses = theses, TopDilemmas = topDilemmas };

            return View(model);
        }
    }
}
