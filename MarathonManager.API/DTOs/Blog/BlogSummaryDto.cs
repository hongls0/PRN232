namespace MarathonManager.API.DTOs.Blog
{
    // DTO này dùng để hiển thị blog tóm tắt ra trang chủ
    public class BlogSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FeaturedImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        // Lấy một đoạn tóm tắt ngắn từ Content
        public string Summary { get; set; }
    }
}