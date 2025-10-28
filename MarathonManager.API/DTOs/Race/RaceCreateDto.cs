using System.ComponentModel.DataAnnotations;

namespace MarathonManager.API.DTOs.Race
{
    // DTO này dùng khi Organizer tạo giải mới
    public class RaceCreateDto
    {
        [Required(ErrorMessage = "Tên giải chạy không được để trống")]
        [StringLength(200, ErrorMessage = "Tên giải chạy không quá 200 ký tự")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Địa điểm không được để trống")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Ngày tổ chức không được để trống")]
        public DateTime RaceDate { get; set; }

        public string? ImageUrl { get; set; }

        // Dùng để tạo các cự ly ngay khi tạo giải
        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 cự ly chạy")]
        public List<RaceDistanceCreateDto> Distances { get; set; }
    }

    // DTO phụ cho cự ly
    public class RaceDistanceCreateDto
    {
        [Required]
        public string Name { get; set; } // "Half Marathon"
        [Range(1, 100)]
        public decimal DistanceInKm { get; set; } // 21.0
        [Range(0, double.MaxValue)]
        public decimal RegistrationFee { get; set; }
        [Range(1, 100000)]
        public int MaxParticipants { get; set; }
        public DateTime StartTime { get; set; }
    }
}