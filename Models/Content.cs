using System.ComponentModel.DataAnnotations;

namespace ManualApp.Models
{
    public class Content
    {
        public int ContentId { get; set; }
        
        [StringLength(200, ErrorMessage = "文字数は200文字以内で入力してください。")]
        public string? Text { get; set; }
        
        public int Order { get; set; } = 1;

        // Manualとの関係
        public int ManualId { get; set; }
        public Manual Manual { get; set; } = default!;

        // 作成者のID（IdentityUser の主キー）
        public string CreatorId { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ナビゲーション
        public ApplicationUser? Creator { get; set; }

        // Imageとの1対1関係（null可）
        public Image? Image { get; set; }
    }
}
