using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MoralCompass.Infrastructure.Domain;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly MoralCompassDbContext _context;

    public UserRepository(MoralCompassDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
}

