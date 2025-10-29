// Trong MarathonManager.API/DTOs/Admin/UserDto.cs

namespace MarathonManager.API.DTOs.Admin
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; }
    }
}