using System.Linq.Expressions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Repositories;

public class MainPageRepository: IMainPageRepository
{
    private readonly MoralCompassDbContext _dbContext;

    public MainPageRepository(MoralCompassDbContext dbContext)
    {
        _dbContext = dbContext;    
    }
    
    public async Task<List<CoreThesis>> GetAllCoreThesisAsync()
    {
        return await _dbContext.CoreTheses.ToListAsync();
    }

    public async Task<List<Comment>> GetAllCommnetAsync()
    {
        return await _dbContext.Comments.ToListAsync();
    }

    public async Task<List<DiscussionItem>> GetAllDiscussionItemsAsync()
    {
        return await _dbContext.DiscussionItems.ToListAsync();
    }
    
    public async Task<List<DiscussionItem>> GetDiscussionItemsWithAuthors(
        Expression<Func<DiscussionItem, bool>> filter)
    {
        return await _dbContext.DiscussionItems
            .Where(filter)
            .Include(di => di.Author)
            .ToListAsync();
    }
}