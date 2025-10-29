namespace MarathonManager.Web.DTOs
{
    // DTO này chứa thông tin 1 cự ly
    public class RaceDistanceDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal DistanceInKm { get; set; }
        public decimal RegistrationFee { get; set; }
        public int MaxParticipants { get; set; }
        public DateTime StartTime { get; set; }
    }
}