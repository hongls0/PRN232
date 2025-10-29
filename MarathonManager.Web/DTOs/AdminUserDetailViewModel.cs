using MarathonManager.Web.DTOs;
using System.Collections.Generic;

namespace MarathonManager.Web.ViewModels
{
    public class AdminUserDetailViewModel
    {
        // Thông tin của user đang xem
        public UserDto User { get; set; }

        // Danh sách TẤT CẢ các role (để tạo checkbox)
        public List<RoleDto> AllRoles { get; set; }

        public AdminUserDetailViewModel()
        {
            User = new UserDto();
            AllRoles = new List<RoleDto>();
        }
    }
}