using Microsoft.AspNetCore.Authorization;
using ManualApp.Models;
using ManualApp.Services;
using ManualApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ManualApp.Security
{
    public class GroupAccessHandler : AuthorizationHandler<GroupAccessRequirement>
    {
        private readonly ICurrentUserService _current;
        private readonly ManualAppContext _context;

        public GroupAccessHandler(ICurrentUserService current, ManualAppContext context)
        {
            _current = current;
            _context = context;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GroupAccessRequirement requirement)
        {
            if (!_current.IsAuthenticated)
            {
                return;
            }

            // 管理者は常にアクセス可能
            if (_current.IsAdmin)
            {
                context.Succeed(requirement);
                return;
            }

            // ユーザーがグループに参加しているかチェック
            var user = await _context.Users
                .Include(u => u.Group)
                .FirstOrDefaultAsync(u => u.Id == _current.UserId);

            if (user?.GroupId == null)
            {
                // グループに参加していない場合は、自分のコンテンツのみアクセス可能
                if (requirement.ResourceOwnerId == _current.UserId)
                {
                    context.Succeed(requirement);
                }
                return;
            }

            // グループメンバーの場合
            if (user.GroupId == requirement.TargetGroupId)
            {
                // 権限チェック
                var hasPermission = CheckPermission(user.GroupPermission, requirement.RequiredPermission);
                if (hasPermission)
                {
                    context.Succeed(requirement);
                }
            }
        }

        private bool CheckPermission(GroupPermission? userPermission, GroupPermission requiredPermission)
        {
            if (userPermission == null) return false;

            // 権限の階層: ViewOnly < PartialEdit < FullEdit
            return userPermission >= requiredPermission;
        }
    }
}
