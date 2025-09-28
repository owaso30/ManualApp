namespace ManualApp.Models
{
    public enum JoinRequestStatus
    {
        Pending = 0,    // 申請中
        Approved = 1,   // 承認済み
        Rejected = 2    // 拒否済み
    }

    public class GroupJoinRequest
    {
        public int GroupJoinRequestId { get; set; }
        
        // グループID
        public int GroupId { get; set; }
        
        // 申請者ID
        public string RequesterId { get; set; } = default!;
        
        // 申請メッセージ
        public string? Message { get; set; }
        
        // 申請状態
        public JoinRequestStatus Status { get; set; } = JoinRequestStatus.Pending;
        
        // 申請日時
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        
        // 処理日時
        public DateTime? ProcessedAt { get; set; }
        
        // 処理者ID（グループ管理者）
        public string? ProcessedById { get; set; }
        
        // ナビゲーション
        public Group Group { get; set; } = default!;
        public ApplicationUser Requester { get; set; } = default!;
        public ApplicationUser? ProcessedBy { get; set; }
    }
}
