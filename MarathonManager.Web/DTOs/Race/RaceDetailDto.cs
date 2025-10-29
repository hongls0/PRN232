namespace MarathonManager.Web.DTOs
{
    // DTO này chứa chi tiết 1 giải chạy
    public class RaceDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime RaceDate { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; }
        public string OrganizerName { get; set; }
        public List<RaceDistanceDto> Distances { get; set; } = new List<RaceDistanceDto>();
    }
}