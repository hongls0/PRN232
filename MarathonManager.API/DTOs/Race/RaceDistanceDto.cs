namespace MarathonManager.API.DTOs.Race
{
    // DTO này định nghĩa thông tin cho MỘT cự ly (ví dụ: 5km, 10km...)
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