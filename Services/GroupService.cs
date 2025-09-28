using ManualApp.Data;
using ManualApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ManualApp.Services
{
    public class GroupService : IGroupService
    {
        private readonly ManualAppContext _context;
        private readonly IEmailSender _emailSender;

        public GroupService(ManualAppContext context, IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task<Group> CreateGroupAsync(string name, string? description, string ownerId)
        {
            var groupCode = await GenerateGroupCodeAsync();
            
            var group = new Group
            {
                Name = name,
                Description = description,
                GroupCode = groupCode,
                OwnerId = ownerId ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            // 作成者をグループメンバーとして追加（管理者権限）
            var membership = new GroupMembership
            {
                GroupId = group.GroupId,
                UserId = ownerId,
                Permission = GroupPermission.FullEdit,
                JoinedAt = DateTime.UtcNow
            };

            _context.GroupMemberships.Add(membership);

            // ユーザーの現在のグループ情報を更新
            var user = await _context.Users.FindAsync(ownerId);
            if (user != null)
            {
                user.GroupId = group.GroupId;
                user.GroupPermission = GroupPermission.FullEdit;
            }

            await _context.SaveChangesAsync();

            return group;
        }

        public async Task<string> GenerateGroupCodeAsync()
        {
            string groupCode;
            bool isUnique;
            
            do
            {
                var randomPart = GenerateRandomString(12);
                groupCode = $"G-{randomPart}";
                isUnique = !await _context.Groups.AnyAsync(g => g.GroupCode == groupCode);
            } while (!isUnique);

            return groupCode;
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<Group?> GetGroupByCodeAsync(string groupCode)
        {
            return await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Memberships)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.GroupCode == groupCode);
        }

        public async Task<Group?> GetGroupByIdAsync(int groupId)
        {
            return await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Memberships)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);
        }

        public async Task<List<Group>> GetUserOwnedGroupsAsync(string userId)
        {
            return await _context.Groups
                .Where(g => g.OwnerId == userId)
                .Include(g => g.Memberships)
                .ToListAsync();
        }

        private GroupMembership? _cachedUserMembership = null;
        private string? _lastMembershipUserId = null;

        public async Task<GroupMembership?> GetUserMembershipAsync(string userId)
        {
            try
            {
                // ユーザーが変わった場合はキャッシュをクリア
                if (_lastMembershipUserId != userId)
                {
                    _cachedUserMembership = null;
                    _lastMembershipUserId = userId;
                }

                // キャッシュがない場合のみデータベースアクセス
                if (_cachedUserMembership == null)
                {
                    _cachedUserMembership = await _context.GroupMemberships
                        .Include(gm => gm.Group)
                        .FirstOrDefaultAsync(gm => gm.UserId == userId);
                }

                return _cachedUserMembership;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> RequestToJoinGroupAsync(string groupCode, string requesterId, string? message = null)
        {
            var group = await GetGroupByCodeAsync(groupCode);
            if (group == null) return false;

            // 既に参加申請中または既にメンバーの場合は拒否
            var existingRequest = await _context.GroupJoinRequests
                .FirstOrDefaultAsync(gjr => gjr.GroupId == group.GroupId && gjr.RequesterId == requesterId && gjr.Status == JoinRequestStatus.Pending);
            
            if (existingRequest != null) return false;

            var existingMembership = await _context.GroupMemberships
                .FirstOrDefaultAsync(gm => gm.GroupId == group.GroupId && gm.UserId == requesterId);
            
            if (existingMembership != null) return false;

            var joinRequest = new GroupJoinRequest
            {
                GroupId = group.GroupId,
                RequesterId = requesterId,
                Message = message,
                Status = JoinRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _context.GroupJoinRequests.Add(joinRequest);
            await _context.SaveChangesAsync();

            // グループ管理者にメール通知
            var owner = await _context.Users.FindAsync(group.OwnerId);
            if (owner != null)
            {
                if (_emailSender is EmailSender emailSender)
                {
                    await emailSender.SendGroupJoinRequestNotificationAsync(owner.Email!, group.Name, requesterId);
                }
                else if (_emailSender is ProductionEmailSender productionEmailSender)
                {
                    await productionEmailSender.SendGroupJoinRequestNotificationAsync(owner.Email!, group.Name, requesterId);
                }
            }

            return true;
        }

        private List<GroupJoinRequest>? _cachedPendingRequests = null;
        private string? _lastPendingRequestsUserId = null;

        public async Task<List<GroupJoinRequest>> GetPendingJoinRequestsAsync(string userId)
        {
            try
            {
                // ユーザーが変わった場合はキャッシュをクリア
                if (_lastPendingRequestsUserId != userId)
                {
                    _cachedPendingRequests = null;
                    _lastPendingRequestsUserId = userId;
                }

                // キャッシュがない場合のみデータベースアクセス
                if (_cachedPendingRequests == null)
                {
                    // ユーザーがオーナーのグループに対する参加申請を取得
                    var ownedGroupIds = await _context.Groups
                        .Where(g => g.OwnerId == userId)
                        .Select(g => g.GroupId)
                        .ToListAsync();

                    _cachedPendingRequests = await _context.GroupJoinRequests
                        .Include(gjr => gjr.Requester)
                        .Include(gjr => gjr.Group)
                        .Where(gjr => ownedGroupIds.Contains(gjr.GroupId) && gjr.Status == JoinRequestStatus.Pending)
                        .OrderByDescending(gjr => gjr.RequestedAt)
                        .ToListAsync();
                }

                return _cachedPendingRequests;
            }
            catch (Exception)
            {
                return new List<GroupJoinRequest>();
            }
        }

        public async Task<bool> ProcessJoinRequestAsync(int requestId, string processorId, bool approved, GroupPermission? permission = null)
        {
            var request = await _context.GroupJoinRequests
                .Include(gjr => gjr.Group)
                .FirstOrDefaultAsync(gjr => gjr.GroupJoinRequestId == requestId);

            if (request == null || request.Group.OwnerId != processorId) return false;

            request.Status = approved ? JoinRequestStatus.Approved : JoinRequestStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedById = processorId;

            if (approved)
            {
                // 申請者の現在の状況をチェック
                var user = await _context.Users.FindAsync(request.RequesterId);
                if (user == null) return false;

                // 既に他のグループに所属しているかチェック
                if (user.GroupId != null)
                {
                    // 既にグループに所属している場合は承認を拒否
                    request.Status = JoinRequestStatus.Rejected;
                    request.ProcessedAt = DateTime.UtcNow;
                    request.ProcessedById = processorId;
                    await _context.SaveChangesAsync();
                    return false;
                }

                // 自分でグループを作成しているかチェック
                var ownedGroups = await _context.Groups
                    .Where(g => g.OwnerId == request.RequesterId)
                    .ToListAsync();
                
                if (ownedGroups.Any())
                {
                    // 自分でグループを作成している場合は承認を拒否
                    request.Status = JoinRequestStatus.Rejected;
                    request.ProcessedAt = DateTime.UtcNow;
                    request.ProcessedById = processorId;
                    await _context.SaveChangesAsync();
                    return false;
                }

                // メンバーシップを追加
                var membership = new GroupMembership
                {
                    GroupId = request.GroupId,
                    UserId = request.RequesterId,
                    Permission = permission ?? GroupPermission.ViewOnly,
                    JoinedAt = DateTime.UtcNow
                };

                _context.GroupMemberships.Add(membership);

                // ユーザーの現在のグループ情報を更新
                user.GroupId = request.GroupId;
                user.GroupPermission = permission ?? GroupPermission.ViewOnly;

                // 申請者にメール通知
                if (user != null)
                {
                    if (_emailSender is EmailSender emailSender)
                    {
                        await emailSender.SendGroupJoinApprovalNotificationAsync(user.Email!, request.Group.Name, approved);
                    }
                    else if (_emailSender is ProductionEmailSender productionEmailSender)
                    {
                        await productionEmailSender.SendGroupJoinApprovalNotificationAsync(user.Email!, request.Group.Name, approved);
                    }
                }
            }
            else
            {
                // 申請者にメール通知
                var user = await _context.Users.FindAsync(request.RequesterId);
                if (user != null)
                {
                    if (_emailSender is EmailSender emailSender)
                    {
                        await emailSender.SendGroupJoinApprovalNotificationAsync(user.Email!, request.Group.Name, approved);
                    }
                    else if (_emailSender is ProductionEmailSender productionEmailSender)
                    {
                        await productionEmailSender.SendGroupJoinApprovalNotificationAsync(user.Email!, request.Group.Name, approved);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveMemberAsync(int groupId, string userId, string processorId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null || group.OwnerId != processorId) return false;

            var membership = await _context.GroupMemberships
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (membership == null) return false;

            _context.GroupMemberships.Remove(membership);

            // ユーザーの現在のグループ情報をクリア
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.GroupId == groupId)
            {
                user.GroupId = null;
                user.GroupPermission = null;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateMemberPermissionAsync(int groupId, string userId, GroupPermission permission, string processorId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null || group.OwnerId != processorId) return false;

            var membership = await _context.GroupMemberships
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);

            if (membership == null) return false;

            membership.Permission = permission;

            // ユーザーの現在のグループ権限を更新
            var user = await _context.Users.FindAsync(userId);
            if (user != null && user.GroupId == groupId)
            {
                user.GroupPermission = permission;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GroupMembership>> GetGroupMembersAsync(int groupId)
        {
            return await _context.GroupMemberships
                .Include(gm => gm.User)
                .Where(gm => gm.GroupId == groupId)
                .ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetGroupMemberUsersAsync(int groupId)
        {
            return await _context.GroupMemberships
                .Include(gm => gm.User)
                .Where(gm => gm.GroupId == groupId)
                .Select(gm => gm.User)
                .ToListAsync();
        }

        public async Task<bool> LeaveGroupAsync(string userId)
        {
            var membership = await _context.GroupMemberships
                .Include(gm => gm.Group)
                .FirstOrDefaultAsync(gm => gm.UserId == userId);

            if (membership == null) return false;

            // グループオーナーは脱退できない
            if (membership.Group.OwnerId == userId) return false;

            _context.GroupMemberships.Remove(membership);

            // ユーザーの現在のグループ情報をクリア
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.GroupId = null;
                user.GroupPermission = null;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsGroupOwnerAsync(int groupId, string userId)
        {
            return await _context.Groups.AnyAsync(g => g.GroupId == groupId && g.OwnerId == userId);
        }

        public async Task<bool> IsGroupMemberAsync(int groupId, string userId)
        {
            return await _context.GroupMemberships.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        }

        public async Task<bool> HasPermissionAsync(string userId, GroupPermission requiredPermission)
        {
            var membership = await _context.GroupMemberships
                .FirstOrDefaultAsync(gm => gm.UserId == userId);

            if (membership == null) return false;

            return membership.Permission >= requiredPermission;
        }

        public async Task<bool> DeleteGroupAsync(int groupId, string ownerId)
        {
            var group = await _context.Groups
                .Include(g => g.Memberships)
                .Include(g => g.JoinRequests)
                .FirstOrDefaultAsync(g => g.GroupId == groupId);

            if (group == null || group.OwnerId != ownerId)
            {
                return false;
            }

            // グループに関連するデータを削除
            _context.GroupJoinRequests.RemoveRange(group.JoinRequests);
            _context.GroupMemberships.RemoveRange(group.Memberships);
            
            // グループメンバーのGroupIdとGroupPermissionをnullに設定
            var memberUsers = await _context.Users
                .Where(u => u.GroupId == groupId)
                .ToListAsync();
            
            foreach (var user in memberUsers)
            {
                user.GroupId = null;
                user.GroupPermission = null;
            }

            // グループを削除
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
