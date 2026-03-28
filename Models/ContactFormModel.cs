using System.ComponentModel.DataAnnotations;

namespace TINH_FINAL_2256.Models
{
    public class ContactFormModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? Subject { get; set; }

        [Required]
        public string Message { get; set; }
    }
}
