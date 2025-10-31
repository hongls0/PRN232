using MarathonManager.Web.DTOs;

using System.Collections.Generic;

namespace MarathonManager.Web.ViewModels
{
    public class AdminUserDetailsViewModel
    {
        // 1. Thông tin chi tiết của user đang xem
        public UserDto User { get; set; }

        // 2. Danh sách TẤT CẢ các Role có trong hệ thống (để làm checkbox)
        public List<RoleDto> AllRoles { get; set; }

        // Hàm khởi tạo để đảm bảo các đối tượng không bao giờ bị null
        public AdminUserDetailsViewModel()
        {
            User = new UserDto();
            AllRoles = new List<RoleDto>();
        }
    }
}