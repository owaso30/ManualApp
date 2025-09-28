namespace ManualApp.Models
{
    public enum GroupPermission
    {
        ViewOnly = 0,        // 閲覧可
        PartialEdit = 1,     // 一部編集可
        FullEdit = 2         // 編集可
    }

    public class GroupMembership
    {
        public int GroupMembershipId { get; set; }
        
        // グループID
        public int GroupId { get; set; }
        
        // ユーザーID
        public string UserId { get; set; } = default!;
        
        // 権限
        public GroupPermission Permission { get; set; } = GroupPermission.ViewOnly;
        
        // 参加日時
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        
        // ナビゲーション
        public Group Group { get; set; } = default!;
        public ApplicationUser User { get; set; } = default!;
    }
}
