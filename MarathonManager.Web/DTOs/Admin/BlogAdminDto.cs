// Trong MarathonManager.Web/DTOs/BlogAdminDto.cs
namespace MarathonManager.Web.DTOs
{
    public class BlogAdminDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public string AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}