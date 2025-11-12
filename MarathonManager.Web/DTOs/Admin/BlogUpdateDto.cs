using System.ComponentModel.DataAnnotations;
namespace MarathonManager.Web.DTOs.Admin
{
    public class BlogUpdateDto
    {
        [Required]
        public int Id { get; set; }

        [Display(Name = "Tiêu đề")]
        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        public string Title { get; set; }

        [Display(Name = "Nội dung")]
        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        public string Content { get; set; }

        [Display(Name = "Trạng thái")]
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public string Status { get; set; }

        // Dùng để hiển thị ảnh hiện tại (nếu có)
        public string? CurrentImageUrl { get; set; }
    }
}