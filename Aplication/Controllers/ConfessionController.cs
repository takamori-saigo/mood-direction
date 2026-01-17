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
    
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCaseConfession(Guid id)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var dilemma = await _context.DiscussionItems
            .Include(di => di.Topic)
            .FirstOrDefaultAsync(di => di.Id == id && di.Type == DiscussionItemType.Dilemma);

        if (dilemma == null) return NotFound();
        if (dilemma.AuthorId != userId && !IsAdmin()) return Forbid();

        var topicId = dilemma.TopicId;
        _context.DiscussionItems.Remove(dilemma);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index", "Confession");
    }
    
    private bool IsAdmin()
    {
        return User.IsInRole("Admin") || 
               (User.Identity?.IsAuthenticated == true && 
                bool.TryParse(User.FindFirst("IsAdmin")?.Value, out var isAdmin) && isAdmin);
    }
    
    public async Task<IActionResult> Details(Guid id)
    {
        if (id == Guid.Empty)
            return NotFound();

        var item = await _context.DiscussionItems
            .FirstOrDefaultAsync(di => di.Id == id);
        if (item == null) return NotFound();

        Guid? editCommentId = null;
        if (Guid.TryParse(Request.Query["editCommentId"], out var parsedEditId))
        {
            editCommentId = parsedEditId;
        }
        
        
        
        var author = await _context.Users.FindAsync(item.AuthorId);
        var topic = item.TopicId != Guid.Empty
            ? await _context.Topics
                .Include(t => t.CoreThesis)
                .FirstOrDefaultAsync(t => t.Id == item.TopicId)
            : null;

        var comments = await _context.Comments
            .Where(c => c.DiscussionItemId == id)
            .Include(c => c.Author)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        
        var commentIds = comments.Select(c => c.Id).ToList();

        var reactions = await _context.Reactions
            .Where(r => r.TargetType == ReactionTargetType.Comment && commentIds.Contains(r.TargetId))
            .ToListAsync();

        var reactionsByComment = reactions
            .GroupBy(r => r.TargetId)
            .ToDictionary(
                g => g.Key,
                g => new { Likes = g.Count(x => x.Value == 1), Dislikes = g.Count(x => x.Value == -1) });

        var userReactionMap = new Dictionary<Guid, int>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var userId))
            {
                var userReactions = await _context.Reactions
                    .Where(r => r.UserId == userId &&
                               r.TargetType == ReactionTargetType.Comment &&
                               commentIds.Contains(r.TargetId))
                    .ToDictionaryAsync(r => r.TargetId, r => r.Value);
                userReactionMap = userReactions;
            }
        }

        Guid? currentUserId = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var parsedUserId))
            {
                currentUserId = parsedUserId;
                
                var userReactions = await _context.Reactions
                    .Where(r => r.UserId == parsedUserId &&
                                r.TargetType == ReactionTargetType.Comment &&
                                commentIds.Contains(r.TargetId))
                    .ToDictionaryAsync(r => r.TargetId, r => r.Value);
                userReactionMap = userReactions;
            }
        }

        bool isAdmin = false;
        if (currentUserId.HasValue)
        {
            var user = await _context.Users.FindAsync(currentUserId.Value);
            isAdmin = user?.IsAdmin == true;
        }

        var commentViewModels = comments.Select(c => new DiscussionItemController.CommentViewModel
        {
            Comment = c,
            AuthorNickname = c.Author?.Nickname ?? "Аноним",
            Likes = reactionsByComment.GetValueOrDefault(c.Id, new { Likes = 0, Dislikes = 0 }).Likes,
            Dislikes = reactionsByComment.GetValueOrDefault(c.Id, new { Likes = 0, Dislikes = 0 }).Dislikes,
            UserReaction = userReactionMap.GetValueOrDefault(c.Id, 0),
            CanEdit = currentUserId.HasValue && (c.AuthorId == currentUserId.Value || isAdmin),
            CanDelete = currentUserId.HasValue && (c.AuthorId == currentUserId.Value || isAdmin),
            IsEditing = editCommentId == c.Id && currentUserId.HasValue && (c.AuthorId == currentUserId.Value || isAdmin)
        }).ToList();

        var dilemmaReactions = await _context.Reactions
            .Where(r => r.TargetType == ReactionTargetType.DiscussionItem && r.TargetId == id)
            .ToListAsync();
        var canEditOrDelete = currentUserId.HasValue && 
                              (item.AuthorId == currentUserId.Value || isAdmin);
        var likeCount = dilemmaReactions.Count(r => r.Value == 1);
        var dislikeCount = dilemmaReactions.Count(r => r.Value == -1);
        var userDilemmaReaction = dilemmaReactions
            .FirstOrDefault(r => r.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)?.Value;

        var model = new DiscussionItemController.DiscussionItemDetailModel
        {
            Item = item,
            Author = author ?? new User { Nickname = "Аноним" },
            Topic = topic,
            CoreThesis = topic?.CoreThesis,
            Comments = commentViewModels,
            LikeCount = likeCount,
            DislikeCount = dislikeCount,
            UserReactionValue = userDilemmaReaction,
            CanEditOrDelete = canEditOrDelete
        };

        return View(model);
    }
}