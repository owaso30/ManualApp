namespace ManualApp.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsDefault { get; set; } = false; // デフォルトカテゴリーかどうか
        public string? OwnerId { get; set; } // nullの場合は全ユーザー共通

        // ナビゲーション
        public ApplicationUser? Owner { get; set; }
        public List<Manual> Manuals { get; set; } = new();
    }
}
