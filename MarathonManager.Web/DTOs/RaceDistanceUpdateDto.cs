// In MarathonManager.Web/DTOs/RaceDistanceUpdateDto.cs
using System;
using System.ComponentModel.DataAnnotations;
using MarathonManager.Web.Interfaces; // <-- Add this using

namespace MarathonManager.Web.DTOs
{
    public class RaceDistanceUpdateDto : IDistanceFormModel // <-- Implement interface
    {
        public int Id { get; set; }
        public int RaceId { get; set; }

        // Properties required by the interface (already defined with correct attributes)
        public string Name { get; set; }
        public decimal DistanceInKm { get; set; }
        public decimal RegistrationFee { get; set; }
        public int MaxParticipants { get; set; }
        public DateTime StartTime { get; set; }
    }
}