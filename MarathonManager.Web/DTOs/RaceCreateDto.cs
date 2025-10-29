namespace MarathonManager.Web.DTOs
{
    // DTO này dùng để gửi dữ liệu TẠO MỚI giải chạy
    public class RaceCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime RaceDate { get; set; }
        public string ImageUrl { get; set; }
    }
}