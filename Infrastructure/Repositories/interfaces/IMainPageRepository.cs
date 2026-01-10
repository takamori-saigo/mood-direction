using System.Linq.Expressions;
using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Repositories;

public interface IMainPageRepository
{
    Task<List<CoreThesis>> GetAllCoreThesisAsync();
    Task<List<Comment>> GetAllCommnetAsync();
    Task<List<DiscussionItem>> GetDiscussionItemsWithAuthors(Expression<Func<DiscussionItem, bool>> filte);
}