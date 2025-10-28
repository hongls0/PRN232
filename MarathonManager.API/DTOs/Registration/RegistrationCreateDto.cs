using System.ComponentModel.DataAnnotations;
namespace MarathonManager.API.DTOs.Registration
{
    public class RegistrationCreateDto
    {
        [Required]
        public int RaceDistanceId { get; set; }
    }
}