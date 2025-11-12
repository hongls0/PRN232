using System.ComponentModel.DataAnnotations;

namespace MarathonManager.API.DTOs.Admin
{
    public class BlogCreateDto
    {
        [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Nội dung là bắt buộc")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        public string Status { get; set; } // "Draft" or "Published"
    }
}