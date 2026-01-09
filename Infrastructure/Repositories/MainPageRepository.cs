using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Repositories;

public class MainPageRepository: IMainPageRepository
{
    public Task<List<CoreThesis>> GetAllCoreThesisAsync()
    {
        
    }

    public Task<List<Comment>> GetAllCommnetAsync()
    {
        throw new NotImplementedException();
    }

    public Task<DiscussionItem> GetAllDiscussionItemAsync()
    {
        throw new NotImplementedException();
    }
}