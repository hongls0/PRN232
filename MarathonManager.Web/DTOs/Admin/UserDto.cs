// Trong MarathonManager.Web/DTOs/UserDto.cs

namespace MarathonManager.Web.DTOs
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