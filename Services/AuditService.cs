using InternalMarksSystem.Data;
using InternalMarksSystem.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace InternalMarksSystem.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuditService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task LogAsync(string action, string? entityName = null, string? entityId = null, string? details = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                string? userId = null;
                string? userName = null;
                string? userRole = null;
                string? ipAddress = null;

                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    userName = httpContext.User.Identity.Name;

                    var roles = httpContext.User.FindAll(ClaimTypes.Role).Select(r => r.Value);
                    userRole = string.Join(", ", roles);

                    ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                }

                var log = new AuditLog
                {
                    UserId = userId,
                    UserName = userName ?? "System",
                    UserRole = string.IsNullOrEmpty(userRole) ? "System" : userRole,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Details = details,
                    TimestampUtc = DateTime.UtcNow,
                    IpAddress = ipAddress
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Never allow audit logging failure to crash primary business flow
            }
        }
    }
}
