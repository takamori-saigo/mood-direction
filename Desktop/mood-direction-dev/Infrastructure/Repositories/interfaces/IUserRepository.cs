using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
}