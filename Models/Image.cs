namespace ManualApp.Models
{
    public class Image
    {
        public int ImageId { get; set; }
        public required string FilePath { get; set; }

        // Contentとの1対1関係（null可）
        public int? ContentId { get; set; }
        public Content? Content { get; set; }
    }
}
