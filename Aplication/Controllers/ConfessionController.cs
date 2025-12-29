using System.Security.Claims;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoralCompass.Infrastructure.Domain;
using MoralCompass.Infrastructure.Domain.Enums;

namespace MoralCompass.Web.Controllers;

public class ConfessionController : Controller
{
    private readonly MoralCompassDbContext _context;

    public ConfessionController(MoralCompassDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // 🔹 ИЗМЕНЕНО: di.TopicId == null (а не Guid.Empty)
        var confessions = await _context.DiscussionItems
            .Where(di => di.TopicId == null && di.Type == DiscussionItemType.Dilemma)
            .Include(di => di.Author)
            .OrderByDescending(di => di.CreatedAt)
            .Take(30)
            .ToListAsync();

        var confessionIds = confessions.Select(di => di.Id).ToList();
        var commentCounts = await _context.Comments
            .Where(c => confessionIds.Contains(c.DiscussionItemId))
            .GroupBy(c => c.DiscussionItemId)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        ViewBag.Confessions = confessions;
        ViewBag.CommentCounts = commentCounts;

        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError("", "Описание обязательно");
            return View();
        }

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                ?? throw new InvalidOperationException("Пользователь не авторизован"));

        var dilemma = new DiscussionItem
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(title) ? "Не мудак ли я?" : title.Trim(),
            Content = content.Trim(),
            Type = DiscussionItemType.Dilemma,
            AuthorId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        _context.DiscussionItems.Add(dilemma);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "DiscussionItem", new { id = dilemma.Id });
    }
}