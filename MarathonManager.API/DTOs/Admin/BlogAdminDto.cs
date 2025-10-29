// Trong MarathonManager.API/DTOs/Admin/BlogAdminDto.cs
namespace MarathonManager.API.DTOs.Admin
{
    public class BlogAdminDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; } // Draft or Published
        public string AuthorName { get; set; } // Lấy tên người viết
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}