using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ManualApp.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public CurrentUserService(IHttpContextAccessor accessor) => _httpContextAccessor = accessor;

        public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        public bool IsAdmin => _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
    }
}