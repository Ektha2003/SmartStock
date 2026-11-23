using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartStock.Core.Common;
using SmartStock.Core.DTOs;
using SmartStock.Core.Entities;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUserService _currentUserService;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, ICurrentUserService currentUserService)
    {
        _context = context;
        _configuration = configuration;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default)
    {
        var emailNormalized = dto.Email.Trim().ToLowerInvariant();

        var exists = await _context.Users.AnyAsync(u => u.Email.ToLower() == emailNormalized, cancellationToken);
        if (exists)
        {
            throw new ConflictAppException("A user with this email address already exists.");
        }

        // Execute in an ExecutionStrategy to support transactions with retries
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var business = new Business
                {
                    Id = Guid.NewGuid(),
                    BusinessName = dto.BusinessName.Trim(),
                    OwnerName = string.IsNullOrWhiteSpace(dto.OwnerName) ? dto.FullName.Trim() : dto.OwnerName.Trim(),
                    Phone = dto.Phone?.Trim() ?? string.Empty,
                    Address = dto.Address?.Trim() ?? string.Empty,
                    BusinessType = string.IsNullOrWhiteSpace(dto.BusinessType) ? "Kirana" : dto.BusinessType.Trim(),
                    IsActive = true
                };

                _context.Businesses.Add(business);
                await _context.SaveChangesAsync(cancellationToken);

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    BusinessId = business.Id,
                    FullName = dto.FullName.Trim(),
                    Email = emailNormalized,
                    PasswordHash = passwordHash,
                    Role = UserRole.Owner,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync(cancellationToken);

                var token = GenerateJwtToken(user, business.Id);
                var refreshToken = GenerateRefreshToken(user.Id);

                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                var response = new AuthResponseDto
                {
                    Token = token,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenLifetimeMinutes()),
                    User = MapUserToDto(user),
                    Business = MapBusinessToDto(business)
                };

                return ApiResponse<AuthResponseDto>.Ok(response, "Business and owner registered successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default)
    {
        var emailNormalized = dto.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.Business)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == emailNormalized, cancellationToken);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAppException("User account is inactive. Please contact support.");
        }

        if (!user.Business.IsActive)
        {
            throw new UnauthorizedAppException("Business account is deactivated.");
        }

        var token = GenerateJwtToken(user, user.BusinessId);
        var refreshToken = GenerateRefreshToken(user.Id);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenLifetimeMinutes()),
            User = MapUserToDto(user),
            Business = MapBusinessToDto(user.Business)
        };

        return ApiResponse<AuthResponseDto>.Ok(response, "Login successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        var existingToken = await _context.RefreshTokens
            .Include(r => r.User)
            .ThenInclude(u => u.Business)
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken, cancellationToken);

        if (existingToken == null || !existingToken.IsActive)
        {
            throw new UnauthorizedAppException("Invalid or expired refresh token.");
        }

        // Revoke the old token
        existingToken.RevokedAt = DateTime.UtcNow;

        var user = existingToken.User;
        var newJwt = GenerateJwtToken(user, user.BusinessId);
        var newRefreshToken = GenerateRefreshToken(user.Id);
        existingToken.ReplacedByToken = newRefreshToken.Token;

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new AuthResponseDto
        {
            Token = newJwt,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenLifetimeMinutes()),
            User = MapUserToDto(user),
            Business = MapBusinessToDto(user.Business)
        };

        return ApiResponse<AuthResponseDto>.Ok(response, "Token refreshed successfully.");
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAppException("Unauthenticated.");
        }

        var user = await _context.Users.FindAsync([userId.Value], cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        return ApiResponse<UserDto>.Ok(MapUserToDto(user), "Current user retrieved successfully.");
    }

    private string GenerateJwtToken(User user, Guid businessId)
    {
        var secret = _configuration["Jwt:Secret"] ?? "SmartStockSuperSecureJwtSigningKeyForHackathon2026WithSufficientLength!";
        var issuer = _configuration["Jwt:Issuer"] ?? "SmartStockAPI";
        var audience = _configuration["Jwt:Audience"] ?? "SmartStockClient";
        var minutes = GetTokenLifetimeMinutes();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("userId", user.Id.ToString()),
            new("businessId", businessId.ToString()),
            new("role", user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static RefreshToken GenerateRefreshToken(Guid userId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(randomBytes),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
    }

    private int GetTokenLifetimeMinutes()
    {
        return int.TryParse(_configuration["Jwt:AccessTokenExpirationMinutes"], out var min) ? min : 1440;
    }

    private static UserDto MapUserToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            BusinessId = user.BusinessId,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    private static BusinessDto MapBusinessToDto(Business b)
    {
        return new BusinessDto
        {
            Id = b.Id,
            BusinessName = b.BusinessName,
            OwnerName = b.OwnerName,
            Phone = b.Phone,
            Address = b.Address,
            BusinessType = b.BusinessType,
            IsActive = b.IsActive,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }
}
