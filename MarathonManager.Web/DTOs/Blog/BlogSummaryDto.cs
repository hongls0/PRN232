namespace MarathonManager.Web.DTOs
{
    public class BlogSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string FeaturedImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Summary { get; set; }
    }
}