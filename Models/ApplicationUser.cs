using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ManualApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "表示名は必須です")]
        [StringLength(50, ErrorMessage = "表示名は{1}文字以内で入力してください", MinimumLength = 1)]
        [Display(Name = "表示名")]
        public string DisplayName { get; set; } = string.Empty;
        
        // グループ関連フィールド
        public int? GroupId { get; set; } // 参加しているグループID（nullの場合はグループ未参加）
        public GroupPermission? GroupPermission { get; set; } // グループ内での権限（nullの場合はグループ未参加）
        
        // ナビゲーション
        public Group? Group { get; set; }
        public List<GroupMembership> GroupMemberships { get; set; } = new();
        public List<GroupJoinRequest> GroupJoinRequests { get; set; } = new();
        public List<Group> OwnedGroups { get; set; } = new(); // 作成したグループ
    }
}