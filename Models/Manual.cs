namespace ManualApp.Models
{
    public class Manual
    {
        public int ManualId { get; set; }
        public string Title { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // カテゴリとの関係
        public int CategoryId { get; set; }
        public Category Category { get; set; } = default!;

        // 所有者のID（IdentityUser の主キー）
        public string OwnerId { get; set; } = default!;

        // ナビゲーション
        public ApplicationUser? Owner { get; set; }
        public List<Content> Contents { get; set; } = new();
    }
}
