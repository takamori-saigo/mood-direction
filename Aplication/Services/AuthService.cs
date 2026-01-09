using Domain.DTO;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using MoralCompass.Infrastructure.Domain;

namespace Aplication.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing != null) throw new Exception("Email уже используется");
        var user = new User {
            Nickname = request.Nickname,
            Email = request.Email,
            Age = request.Age,
            Gender = request.Gender
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        await _userRepository.AddAsync(user);
        return user;
    }
    
    public async Task<User> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null) throw new Exception("Пользователь не найден");
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new Exception("Неверная почта или пароль");
        return user; 
    }
    
    public async Task<User> GetOrCreateExternalUserAsync(string email, string nickname)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user != null) return user;
        user = new User
        {
            Email = email,
            Nickname = nickname,
            Age = 0,                 // можно потом заполнить
            PasswordHash = string.Empty
        };

        await _userRepository.AddAsync(user);
        return user;
    }

}
