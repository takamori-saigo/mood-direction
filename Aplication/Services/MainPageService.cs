using Infrastructure.Repositories;
using MoralCompass.Infrastructure.Domain;
using MoralCompass.Infrastructure.Domain.Enums;

namespace Aplication.Services;

public class MainPageService
{
    private readonly IMainPageRepository _mainPageRepository;

    public MainPageService(IMainPageRepository mainPageRepository)
    {
        _mainPageRepository = mainPageRepository;
    }

    public async Task<List<Guid>> GetTopDilemmsIdAsync()
    {
        var comments = await _mainPageRepository.GetAllCommnetAsync();
        return comments
            .GroupBy(c => c.DiscussionItemId) // ← безопасно, потому что не null
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.Id)
            .Take(3)
            .Select(x => x.Id)
            .ToList();
    }

    public async Task<List<CoreThesis>> GetAllThesesByActive()
    {
        var these = await _mainPageRepository.GetAllCoreThesisAsync();
        return these.Where(ct => ct.IsActive)
        .OrderBy(ct => ct.Order)
        .ToList();;
    }
    
    public async Task<List<DiscussionItem>> GetTopDilemmasById(List<Guid> ids)
    {
        var dilemmas = await _mainPageRepository.GetDiscussionItemsWithAuthors(
            di => di.Type == DiscussionItemType.Dilemma && ids.Contains(di.Id)
        );

        var dilemmaDict = dilemmas.ToDictionary(di => di.Id);

        var orderedDilemmas = ids
            .Select(id => dilemmaDict.TryGetValue(id, out var di) ? di : null)
            .Where(di => di != null)
            .ToList();

        return orderedDilemmas;
    }
}