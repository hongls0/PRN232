using MarathonManager.API.DTOs.RaceDistances;
using System.ComponentModel.DataAnnotations;

namespace MarathonManager.API.DTOs.Race
{
    public class RaceCreateDto
    {
        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public DateTime RaceDate { get; set; }

        [Required(ErrorMessage = "Phải có ít nhất một cự ly.")]
        public List<int> Distances { get; set; } = new();
        public string? DistancesCsv { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}
