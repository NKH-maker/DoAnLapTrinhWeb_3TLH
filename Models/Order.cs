using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace TINH_FINAL_2256.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        public decimal TotalPrice { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        public string? Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled

        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
