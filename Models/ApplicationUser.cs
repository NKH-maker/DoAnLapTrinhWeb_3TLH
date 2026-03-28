using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TINH_FINAL_2256.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Age { get; set; }
    }
}
