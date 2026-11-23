using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SmartStock.Core.Enums;
using SmartStock.Core.Exceptions;
using SmartStock.Core.Interfaces;

namespace SmartStock.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User?.FindFirst("sub")?.Value 
                          ?? User?.FindFirst("userId")?.Value;
            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public Guid? BusinessId
    {
        get
        {
            var businessClaim = User?.FindFirst("businessId")?.Value 
                                ?? User?.FindFirst("BusinessId")?.Value;
            return Guid.TryParse(businessClaim, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value 
                            ?? User?.FindFirst("email")?.Value;

    public UserRole? Role
    {
        get
        {
            var roleClaim = User?.FindFirst(ClaimTypes.Role)?.Value 
                            ?? User?.FindFirst("role")?.Value;
            return Enum.TryParse<UserRole>(roleClaim, true, out var role) ? role : null;
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid GetRequiredBusinessId()
    {
        var id = BusinessId;
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            throw new UnauthorizedAppException("Business context is required. Please log in again.");
        }
        return id.Value;
    }
}
