// In MarathonManager.Web/Interfaces/IDistanceFormModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace MarathonManager.Web.Interfaces
{
    // Interface defining the common fields for the distance form
    public interface IDistanceFormModel
    {
        // We don't include RaceId or Id here as they are handled differently
        // in Create vs Update

        [Required(ErrorMessage = "Tên cự ly là bắt buộc")]
        [MaxLength(100)]
        [Display(Name = "Tên cự ly")]
        string Name { get; set; }

        [Required(ErrorMessage = "Cự ly (km) là bắt buộc")]
        [Range(0.1, 1000, ErrorMessage = "Cự ly phải lớn hơn 0")]
        [Display(Name = "Cự ly (km)")]
        decimal DistanceInKm { get; set; }

        [Required(ErrorMessage = "Phí đăng ký là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Phí đăng ký không được âm")]
        [Display(Name = "Phí đăng ký (VNĐ)")]
        [DataType(DataType.Currency)]
        decimal RegistrationFee { get; set; }

        [Required(ErrorMessage = "Số lượng VĐV tối đa là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng VĐV phải lớn hơn 0")]
        [Display(Name = "Số VĐV Tối đa")]
        int MaxParticipants { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        [Display(Name = "Giờ xuất phát")]
        [DataType(DataType.DateTime)]
        DateTime StartTime { get; set; }
    }
}