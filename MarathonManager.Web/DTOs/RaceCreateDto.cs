using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MarathonManager.Web.DTOs
{
    public class RaceCreateDto
    {
        [Required(ErrorMessage = "Tên giải chạy là bắt buộc")]
        [Display(Name = "Tên giải chạy")]
        public string Name { get; set; }

        [Display(Name = "Mô tả chi tiết")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Địa điểm là bắt buộc")]
        [Display(Name = "Địa điểm (VD: Sa Pa, Lào Cai)")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Ngày giờ chạy là bắt buộc")]
        [Display(Name = "Ngày giờ chạy")]
        [DataType(DataType.DateTime)]
        public DateTime RaceDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập khoảng cách chạy.")]
        [Display(Name = "Khoảng cách (nhập nhiều cách nhau bằng dấu phẩy, ví dụ: 5,10,21)")]
        public string DistancesInput { get; set; } = string.Empty;
        public string? DistancesCsv { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}
