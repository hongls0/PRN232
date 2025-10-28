using System.ComponentModel.DataAnnotations;
namespace MarathonManager.API.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        public string FullName { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }

        // Bạn có thể thêm các trường khác như DateOfBirth, Gender...
    }
}