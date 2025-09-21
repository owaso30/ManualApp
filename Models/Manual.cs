using System.ComponentModel.DataAnnotations;

namespace ManualApp.Models
{
    public class Manual
    {
        public int ManualId { get; set; }
        [StringLength(30, ErrorMessage = "タイトルは30文字以内で入力してください。")]
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
