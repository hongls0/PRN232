using System.Collections.Generic;

namespace MarathonManager.API.DTOs.Admin
{
    public class UpdateUserRolesDto
    {
        // Danh sách TÊN các role mới mà user sẽ có
        public List<string> RoleNames { get; set; }
    }
}