namespace MarathonManager.API.DTOs.Race
{
    public class RaceSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime RaceDate { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; }
        public string OrganizerName { get; set; }
    }
}