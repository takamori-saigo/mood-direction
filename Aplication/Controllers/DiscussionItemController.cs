using System.Security.Claims;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoralCompass.Infrastructure.Domain;
using MoralCompass.Infrastructure.Domain.Enums;

namespace MoralCompass.Web.Controllers;

public class DiscussionItemController : Controller
{
    public class CommentViewModel
    {
        public Comment Comment { get; set; } = null!;
        public string AuthorNickname { get; set; } = "Аноним";
        public int Likes { get; set; }
        public int Dislikes { get; set; }
        public int UserReaction { get; set; } // 1, -1, 0
    }

    public class DiscussionItemDetailModel
    {
        public DiscussionItem Item { get; set; } = null!;
        public User Author { get; set; } = null!;
        public Topic? Topic { get; set; }
        public CoreThesis? CoreThesis { get; set; }
        public List<CommentViewModel> Comments { get; set; } = new();
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public int? UserReactionValue { get; set; }
    }

    private readonly MoralCompassDbContext _context;

    public DiscussionItemController(MoralCompassDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Details(Guid id)
    {
        if (id == Guid.Empty)
            return NotFound();

        var item = await _context.DiscussionItems
            .FirstOrDefaultAsync(di => di.Id == id);
        if (item == null) return NotFound();

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

        var commentViewModels = comments.Select(c => new CommentViewModel
        {
            Comment = c,
            AuthorNickname = c.Author?.Nickname ?? "Аноним",
            Likes = reactionsByComment.GetValueOrDefault(c.Id, new { Likes = 0, Dislikes = 0 }).Likes,
            Dislikes = reactionsByComment.GetValueOrDefault(c.Id, new { Likes = 0, Dislikes = 0 }).Dislikes,
            UserReaction = userReactionMap.GetValueOrDefault(c.Id, 0)
        }).ToList();

        var dilemmaReactions = await _context.Reactions
            .Where(r => r.TargetType == ReactionTargetType.DiscussionItem && r.TargetId == id)
            .ToListAsync();

        var likeCount = dilemmaReactions.Count(r => r.Value == 1);
        var dislikeCount = dilemmaReactions.Count(r => r.Value == -1);
        var userDilemmaReaction = dilemmaReactions
            .FirstOrDefault(r => r.UserId.ToString() == User.FindFirst(ClaimTypes.NameIdentifier)?.Value)?.Value;

        var model = new DiscussionItemDetailModel
        {
            Item = item,
            Author = author ?? new User { Nickname = "Аноним" },
            Topic = topic,
            CoreThesis = topic?.CoreThesis,
            Comments = commentViewModels,
            LikeCount = likeCount,
            DislikeCount = dislikeCount,
            UserReactionValue = userDilemmaReaction
        };

        return View(model);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddComment(Guid id, string content)
    {
        if (id == Guid.Empty)
            return RedirectToAction("Index", "Index");

        if (string.IsNullOrWhiteSpace(content))
            return RedirectToAction("Details", new { id });

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return RedirectToAction("Login", "Auth");

        try
        {
            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                DiscussionItemId = id,
                AuthorId = userId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AddComment error: {ex}");
            ModelState.AddModelError("", "Не удалось добавить комментарий.");
            return RedirectToAction("Details", new { id });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ToggleReaction(Guid id, int value)
    {
        if (value is not (1 or -1 or 0))
            return BadRequest();

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var existing = await _context.Reactions
            .FirstOrDefaultAsync(r => r.UserId == userId &&
                                   r.TargetType == ReactionTargetType.DiscussionItem &&
                                   r.TargetId == id);

        if (value == 0)
        {
            if (existing != null)
            {
                _context.Reactions.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            if (existing == null)
            {
                _context.Reactions.Add(new Reaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TargetType = ReactionTargetType.DiscussionItem,
                    TargetId = id,
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            else
            {
                existing.Value = value;
            }
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ToggleCommentReaction(Guid commentId, int value)
    {
        if (value is not (1 or -1 or 0))
            return BadRequest();

        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Forbid();

        var existing = await _context.Reactions
            .FirstOrDefaultAsync(r => r.UserId == userId &&
                                   r.TargetType == ReactionTargetType.Comment &&
                                   r.TargetId == commentId);

        if (value == 0)
        {
            if (existing != null)
            {
                _context.Reactions.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            if (existing == null)
            {
                _context.Reactions.Add(new Reaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TargetType = ReactionTargetType.Comment,
                    TargetId = commentId,
                    Value = value,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            else
            {
                existing.Value = value;
            }
            await _context.SaveChangesAsync();
        }

        var comment = await _context.Comments.FindAsync(commentId);
        return RedirectToAction("Details", new { id = comment?.DiscussionItemId ?? Guid.Empty });
    }
}