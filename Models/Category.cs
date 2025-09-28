namespace ManualApp.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = default!;
        public bool IsDefault { get; set; } = false; // デフォルトカテゴリーかどうか
        public string? OwnerId { get; set; } // nullの場合は全ユーザー共通
        public string? CreatorId { get; set; } // 作成者のID（nullの場合はシステム作成）
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 作成日時
        
        // 一部編集権限ユーザーでの編集可否設定
        public bool AllowPartialEdit { get; set; } = true; // 一部編集権限ユーザーでも編集可能かどうか

        // 現在のユーザーがアクセスできるマニュアル数（UI表示用）
        public int ManualCount { get; set; } = 0;

        // ナビゲーション
        public ApplicationUser? Owner { get; set; }
        public ApplicationUser? Creator { get; set; }
        public List<Manual> Manuals { get; set; } = new();
    }
}
