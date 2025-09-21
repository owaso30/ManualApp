namespace ManualApp.Models
{
    public class Content
    {
        public int ContentId { get; set; }
        public string Text { get; set; } = default!;
        public int Order { get; set; } = 0;

        // Manualとの関係
        public int ManualId { get; set; }
        public Manual Manual { get; set; } = default!;

        // Imageとの1対1関係（null可）
        public Image? Image { get; set; }
    }
}
