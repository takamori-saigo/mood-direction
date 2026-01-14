using System.Security.Claims;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoralCompass.Infrastructure.Domain;
using MoralCompass.Infrastructure.Domain.Enums;

namespace MoralCompass.Web.Controllers;

public class TopicController : Controller
{
    public class TopicDetailModel
    {
        public Topic Topic { get; set; } = null!;
        public CoreThesis CoreThesis { get; set; } = null!;
        public List<DiscussionItemWithTopComments> Dilemmas { get; set; } = new();
    }

    public class DiscussionItemWithTopComments
    {
        public DiscussionItem Item { get; set; } = null!;
        public User Author { get; set; } = null!;
        public List<CommentWithScore> TopComments { get; set; } = new();
        public int TotalCommentCount { get; set; }
    }

    public class CommentWithScore
    {
        public Comment Comment { get; set; } = null!;
        public string AuthorNickname { get; set; } = "Аноним";
        public int Score { get; set; } // лайки − дизлайки
    }

    private readonly MoralCompassDbContext _context;

    public TopicController(MoralCompassDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var topic = await _context.Topics
            .Include(t => t.Author)
            .Include(t => t.CoreThesis)
            .FirstOrDefaultAsync(t => t.Id == id);
        
        if (topic == null) return NotFound();
        
        var dilemmas = await _context.DiscussionItems
            .Where(di => di.TopicId == id && di.Type == DiscussionItemType.Dilemma)
            .Include(di => di.Author)
            .OrderByDescending(di => di.CreatedAt)
            .ToListAsync();

        var dilemmaIds = dilemmas.Select(di => di.Id).ToList();

        var comments = await _context.Comments
            .Where(c => dilemmaIds.Contains(c.DiscussionItemId))
            .Include(c => c.Author)
            .ToListAsync();

        var commentIds = comments.Select(c => c.Id).ToList();
        var reactions = await _context.Reactions
            .Where(r => r.TargetType == ReactionTargetType.Comment && commentIds.Contains(r.TargetId))
            .ToListAsync();

        var commentScores = reactions
            .GroupBy(r => r.TargetId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Value));

        var dilemmaModels = dilemmas.Select(di =>
        {
            var diComments = comments
                .Where(c => c.DiscussionItemId == di.Id)
                .Select(c => new CommentWithScore
                {
                    Comment = c,
                    AuthorNickname = c.Author?.Nickname ?? "Аноним",
                    Score = commentScores.GetValueOrDefault(c.Id, 0)
                })
                .OrderByDescending(c => c.Score) // ← самые популярные — первые
                .ThenBy(c => c.Comment.CreatedAt) // при равных — старые выше
                .Take(5)
                .ToList();

            return new DiscussionItemWithTopComments
            {
                Item = di,
                Author = di.Author ?? new User { Nickname = "Аноним" },
                TopComments = diComments,
                TotalCommentCount = comments.Count(c => c.DiscussionItemId == di.Id)
            };
        }).ToList();

        var model = new TopicDetailModel
        {
            Topic = topic,
            CoreThesis = topic.CoreThesis,
            Dilemmas = dilemmaModels
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddDilemma(Guid id, string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            return RedirectToAction("Details", new { id });
        }

        var topic = await _context.Topics.FindAsync(id);
        if (topic == null) return NotFound();

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                                ?? throw new InvalidOperationException("User not authenticated"));

        var dilemma = new DiscussionItem
        {
            Id = Guid.NewGuid(),
            TopicId = id,
            Title = title.Trim(),
            Content = content.Trim(),
            Type = DiscussionItemType.Dilemma,
            AuthorId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.DiscussionItems.Add(dilemma);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id });
    }
    

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTopic(Guid id, string title)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var topic = await _context.Topics
            .Include(t => t.CoreThesis)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        // Проверяем: либо автор, либо админ
        if (topic.AuthorId != userId && !IsAdmin())
            return Forbid();

        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError("Title", "Название темы не может быть пустым.");
            return View(topic);
        }

        topic.Title = title.Trim();
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Thesis", new { id = topic.CoreThesisId });
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditTopic(Guid id)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var topic = await _context.Topics
            .Include(t => t.CoreThesis)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        // Проверяем: либо автор, либо админ
        if (topic.AuthorId != userId && !IsAdmin())
            return Forbid();

        return View(topic); // Показываем форму редактирования
    }
    
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTopic(Guid id)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var topic = await _context.Topics
            .Include(t => t.DiscussionItems)
            .Include(t => t.Author)
            .Include(t => t.CoreThesis)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (topic == null)
            return NotFound();

        // Проверяем: либо автор, либо админ
        if (topic.AuthorId != userId && !IsAdmin())
            return Forbid();

        // Админ может удалять даже если есть дилеммы
        if (topic.DiscussionItems?.Any() == true && !IsAdmin())
        {
            TempData["Error"] = "Невозможно удалить тему, так как в ней есть дилеммы. " +
                                "Только администратор может удалить тему с дилеммами.";
            return RedirectToAction("Details", new { id });
        }

        var thesisId = topic.CoreThesisId;
        var authorName = topic.Author?.Nickname ?? "неизвестен";
        
        _context.Topics.Remove(topic);
        await _context.SaveChangesAsync();

        if (IsAdmin() && topic.AuthorId != userId)
        {
            TempData["Success"] = $"Тема успешно удалена администратором (автор: {authorName})";
        }
        else
        {
            TempData["Success"] = "Тема успешно удалена.";
        }

        return RedirectToAction("Details", "Thesis", new { id = thesisId });
    }
    
    private bool IsAdmin()
    {
        return User.IsInRole("Admin") || 
               (User.Identity?.IsAuthenticated == true && 
                bool.TryParse(User.FindFirst("IsAdmin")?.Value, out var isAdmin) && isAdmin);
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ToggleFavorite(Guid topicId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
        var user = await _context.Users
            .Include(u => u.FavoriteTopics)
            .FirstAsync(u => u.Id == userId);

        var topic = await _context.Topics.FindAsync(topicId);
        if (topic == null) return NotFound();

        if (user.FavoriteTopics.Any(t => t.Id == topicId))
        {
            user.FavoriteTopics.Remove(topic);
        }
        else
        {
            user.FavoriteTopics.Add(topic);
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> EditDilemma(Guid dilemmaId)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var dilemma = await _context.DiscussionItems
            .Include(di => di.Author)
            .FirstOrDefaultAsync(di => di.Id == dilemmaId);

        if (dilemma == null || dilemma.Type != DiscussionItemType.Dilemma)
            return NotFound();

        if (dilemma.AuthorId != userId && !IsAdmin())
            return Forbid();

        return View("EditDilemma", dilemma);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDilemma(Guid dilemmaId, string title, string content)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var dilemma = await _context.DiscussionItems
            .FirstOrDefaultAsync(di => di.Id == dilemmaId && di.Type == DiscussionItemType.Dilemma);

        if (dilemma == null)
            return NotFound();

        if (dilemma.AuthorId != userId && !IsAdmin())
            return Forbid();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError("", "Заголовок и содержание обязательны.");
            return View("EditDilemma", dilemma);
        }

        dilemma.Title = title.Trim();
        dilemma.Content = content.Trim();
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = dilemma.TopicId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDilemma(Guid dilemmaId)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var dilemma = await _context.DiscussionItems
            .Include(di => di.Topic)
            .FirstOrDefaultAsync(di => di.Id == dilemmaId && di.Type == DiscussionItemType.Dilemma);

        if (dilemma == null)
            return NotFound();

        if (dilemma.AuthorId != userId && !IsAdmin())
            return Forbid();

        var topicId = dilemma.TopicId;
        _context.DiscussionItems.Remove(dilemma);
        await _context.SaveChangesAsync();

        TempData["Success"] = IsAdmin() && dilemma.AuthorId != userId
            ? $"Дилемма удалена администратором."
            : "Ваша дилемма успешно удалена.";

        return RedirectToAction("Details", new { id = topicId });
    }
}