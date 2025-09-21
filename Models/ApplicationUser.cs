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
    }
}