using System.ComponentModel.DataAnnotations;

namespace ManualApp.Models
{
    public class Group
    {
        public int GroupId { get; set; }
        
        [Required(ErrorMessage = "グループ名は必須です")]
        [StringLength(50, ErrorMessage = "グループ名は{1}文字以内で入力してください", MinimumLength = 1)]
        [Display(Name = "グループ名")]
        public string Name { get; set; } = default!;
        
        [StringLength(200, ErrorMessage = "説明は{1}文字以内で入力してください")]
        [Display(Name = "説明")]
        public string? Description { get; set; }
        
        public string GroupCode { get; set; } = default!; // G-xxxxxxxxxxxx形式
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // グループ作成者のID
        public string? OwnerId { get; set; }
        
        // ナビゲーション
        public ApplicationUser Owner { get; set; } = default!;
        public List<GroupMembership> Memberships { get; set; } = new();
        public List<GroupJoinRequest> JoinRequests { get; set; } = new();
    }
}
