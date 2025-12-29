using System.Security.Claims;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoralCompass.Infrastructure.Domain;
using MoralCompass.Infrastructure.Domain.Enums;

namespace MoralCompass.Web.Controllers;

public class ThesisController : Controller
{
    public class ThesisDetailModel
    {
        public CoreThesis Thesis { get; set; } = null!;
        public List<TopicWithItems> Topics { get; set; } = new();
    }

    public class TopicWithItems
    {
        public Topic Topic { get; set; } = null!;
    }

    public class DiscussionItemWithComments
    {
        public DiscussionItem Item { get; set; } = null!;
        public List<Comment> Comments { get; set; } = new(); // ← только для дилемм!
    }

    private readonly MoralCompassDbContext _context;

    public ThesisController(MoralCompassDbContext context)
    {
        _context = context;
    }

   
    public async Task<IActionResult> Details(Guid id)
    {
        var thesis = await _context.CoreTheses
            .FirstOrDefaultAsync(ct => ct.Id == id && ct.IsActive);
        if (thesis == null) return NotFound();

        var topics = await _context.Topics
            .Where(t => t.CoreThesisId == id)
            .Include(t => t.Author)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var model = new ThesisDetailModel
        {
            Thesis = thesis,
            Topics = topics.Select(t => new TopicWithItems { Topic = t }).ToList()
        };

        return View(model);
    }
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AttachToTopic(Guid discussionItemId, Guid topicId)
    {
        var item = await _context.DiscussionItems
            .FirstOrDefaultAsync(di => di.Id == discussionItemId && di.TopicId == Guid.Empty);
        if (item == null) return NotFound("Дилемма не найдена или уже привязана.");

        var topic = await _context.Topics.FindAsync(topicId);
        if (topic == null) return BadRequest("Тема не найдена.");

        item.TopicId = topicId;
        _context.Update(item);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = topic.CoreThesisId });
    }

    [HttpGet]
    [Authorize]
    public IActionResult CreateTopic(Guid thesisId)
    {
        var thesis = _context.CoreTheses.Find(thesisId);
        if (thesis == null || !thesis.IsActive) return NotFound();

        ViewBag.Thesis = thesis;
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTopic(Guid thesisId, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError("", "Название темы обязательно");
            ViewBag.Thesis = _context.CoreTheses.Find(thesisId);
            return View();
        }

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                ?? throw new InvalidOperationException("User not authenticated"));

        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            CoreThesisId = thesisId,
            Title = title.Trim(),
            AuthorId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Topics.Add(topic);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = thesisId });
    }
}