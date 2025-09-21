using System.ComponentModel.DataAnnotations;

namespace ManualApp.Models
{
    public class Content
    {
        public int ContentId { get; set; }
        
        [StringLength(200, ErrorMessage = "文字数は200文字以内で入力してください。")]
        public string? Text { get; set; }
        
        public int Order { get; set; } = 0;

        // Manualとの関係
        public int ManualId { get; set; }
        public Manual Manual { get; set; } = default!;

        // Imageとの1対1関係（null可）
        public Image? Image { get; set; }
    }
}
