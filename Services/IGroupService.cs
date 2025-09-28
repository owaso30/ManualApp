using ManualApp.Models;

namespace ManualApp.Services
{
    public interface IGroupService
    {
        Task<Group> CreateGroupAsync(string name, string? description, string ownerId);
        Task<string> GenerateGroupCodeAsync();
        Task<Group?> GetGroupByCodeAsync(string groupCode);
        Task<Group?> GetGroupByIdAsync(int groupId);
        Task<List<Group>> GetUserOwnedGroupsAsync(string userId);
        Task<GroupMembership?> GetUserMembershipAsync(string userId);
        Task<bool> RequestToJoinGroupAsync(string groupCode, string requesterId, string? message = null);
        Task<List<GroupJoinRequest>> GetPendingJoinRequestsAsync(string userId);
        Task<bool> ProcessJoinRequestAsync(int requestId, string processorId, bool approved, GroupPermission? permission = null);
        Task<bool> RemoveMemberAsync(int groupId, string userId, string processorId);
        Task<bool> UpdateMemberPermissionAsync(int groupId, string userId, GroupPermission permission, string processorId);
        Task<List<GroupMembership>> GetGroupMembersAsync(int groupId);
        Task<List<ApplicationUser>> GetGroupMemberUsersAsync(int groupId);
        Task<bool> LeaveGroupAsync(string userId);
        Task<bool> IsGroupOwnerAsync(int groupId, string userId);
        Task<bool> IsGroupMemberAsync(int groupId, string userId);
        Task<bool> HasPermissionAsync(string userId, GroupPermission requiredPermission);
        Task<bool> DeleteGroupAsync(int groupId, string ownerId);
    }
}
