// In MarathonManager.API/DTOs/RaceDistances/RaceDistanceCreateDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace MarathonManager.API.DTOs.RaceDistances
{
    public class RaceDistanceCreateDto
    {
        [Required(ErrorMessage = "Tên cự ly là bắt buộc")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Cự ly (km) là bắt buộc")]
        [Range(0.1, 1000, ErrorMessage = "Cự ly phải lớn hơn 0")]
        public decimal DistanceInKm { get; set; }

        [Required(ErrorMessage = "Phí đăng ký là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Phí đăng ký không được âm")]
        public decimal RegistrationFee { get; set; }

        [Required(ErrorMessage = "Số lượng VĐV tối đa là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng VĐV phải lớn hơn 0")]
        public int MaxParticipants { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        public DateTime StartTime { get; set; }
    }
}