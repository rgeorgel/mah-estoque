using Microsoft.EntityFrameworkCore;
using MahEstoque.Api.Data;
using MahEstoque.Api.DTOs;
using MahEstoque.Api.Models;

namespace MahEstoque.Api.Services;

public interface IUserService
{
    Task<List<UserListItemDto>> GetAllAsync(Guid tenantId);
    Task<UserListItemDto?> GetByIdAsync(Guid id, Guid tenantId);
    Task<UserListItemDto> CreateAsync(CreateUserRequest request, Guid tenantId);
    Task<UserListItemDto> UpdateAsync(Guid id, UpdateUserRequest request, Guid tenantId);
    Task DeleteAsync(Guid id, Guid tenantId);
    Task<bool> IsUsernameUniqueAsync(string username, Guid tenantId, Guid? excludeId = null);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserListItemDto>> GetAllAsync(Guid tenantId)
    {
        return await _context.Users
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.Username)
            .Select(u => new UserListItemDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role.ToString(),
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<UserListItemDto?> GetByIdAsync(Guid id, Guid tenantId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserListItemDto> CreateAsync(CreateUserRequest request, Guid tenantId)
    {
        var user = new User
        {
            TenantId = tenantId,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Email = request.Email,
            Role = Enum.Parse<UserRole>(request.Role, true)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<UserListItemDto> UpdateAsync(Guid id, UpdateUserRequest request, Guid tenantId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null)
            throw new KeyNotFoundException("Usuário não encontrado");

        if (!string.IsNullOrEmpty(request.Email))
            user.Email = request.Email;
        if (!string.IsNullOrEmpty(request.Role))
            user.Role = Enum.Parse<UserRole>(request.Role, true);
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _context.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null)
            throw new KeyNotFoundException("Usuário não encontrado");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsUsernameUniqueAsync(string username, Guid tenantId, Guid? excludeId = null)
    {
        var query = _context.Users.Where(u => u.TenantId == tenantId && u.Username == username);
        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);
        return !await query.AnyAsync();
    }

    private static UserListItemDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt
    };
}