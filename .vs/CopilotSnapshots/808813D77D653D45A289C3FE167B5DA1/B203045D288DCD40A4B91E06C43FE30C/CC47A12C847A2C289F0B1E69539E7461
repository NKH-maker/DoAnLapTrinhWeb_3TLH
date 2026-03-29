using System.ComponentModel.DataAnnotations;

namespace TINH_FINAL_2256.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm tối đa 100 ký tự")]
        public string? Name { get; set; }

        [Range(0.01, 1000000000, ErrorMessage = "Giá phải lớn hơn 0")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }
    }
}