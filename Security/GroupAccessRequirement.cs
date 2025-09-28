using Microsoft.AspNetCore.Authorization;
using ManualApp.Models;

namespace ManualApp.Security
{
    public class GroupAccessRequirement : IAuthorizationRequirement
    {
        public string? ResourceOwnerId { get; set; }
        public int? TargetGroupId { get; set; }
        public GroupPermission RequiredPermission { get; set; }

        public GroupAccessRequirement(string? resourceOwnerId = null, int? targetGroupId = null, GroupPermission requiredPermission = GroupPermission.ViewOnly)
        {
            ResourceOwnerId = resourceOwnerId;
            TargetGroupId = targetGroupId;
            RequiredPermission = requiredPermission;
        }
    }
}
