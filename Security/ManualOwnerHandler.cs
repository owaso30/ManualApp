using Microsoft.AspNetCore.Authorization;
using ManualApp.Models;
using ManualApp.Services;
using ManualApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ManualApp.Security
{
    public class ManualOwnerHandler : AuthorizationHandler<ManualOwnerRequirement, Manual>
    {
        private readonly ICurrentUserService _current;
        private readonly ManualAppContext _context;
        private readonly IModeService _modeService;

        public ManualOwnerHandler(ICurrentUserService current, ManualAppContext context, IModeService modeService)
        {
            _current = current;
            _context = context;
            _modeService = modeService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ManualOwnerRequirement requirement, Manual resource)
        {
            if (_current.IsAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            // 現在のモードに応じたOwnerIDを取得
            var currentOwnerId = await _modeService.GetCurrentOwnerIdAsync();
            
            if (resource.OwnerId == currentOwnerId)
            {
                context.Succeed(requirement);
                return;
            }

            // グループモードの場合、同一グループのメンバーはアクセス可能
            if (_modeService.CurrentMode == ViewMode.Group)
            {
                var user = await _context.Users
                    .Include(u => u.Group)
                    .FirstOrDefaultAsync(u => u.Id == _current.UserId);

                if (user?.GroupId != null && resource.OwnerId != null)
                {
                    var resourceOwner = await _context.Users.FindAsync(resource.OwnerId);
                    if (resourceOwner?.GroupId == user.GroupId)
                    {
                        context.Succeed(requirement);
                    }
                }
            }
        }
    }
}
