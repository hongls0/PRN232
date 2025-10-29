// Trong DTOs/Registration/RunnerRegistrationRequestDto.cs
using System.ComponentModel.DataAnnotations;

namespace MarathonManager.API.DTOs.Registration
{
    // Đổi tên từ RegistrationCreateDto thành RunnerRegistrationRequestDto
    public class RunnerRegistrationRequestDto
    {
        [Required(ErrorMessage = "Vui lòng chọn cự ly.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID cự ly không hợp lệ.")]
        public int RaceDistanceId { get; set; }

        // Bạn có thể thêm các trường khác nếu cần VĐV nhập khi đăng ký
        // Ví dụ:
        // public string? TshirtSize { get; set; }
        // public string? ClubName { get; set; }
    }
}