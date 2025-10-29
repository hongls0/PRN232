// In MarathonManager.Web/DTOs/RaceDistanceCreateDto.cs
using System;
using System.ComponentModel.DataAnnotations;
using MarathonManager.Web.Interfaces; // <-- Add this using

namespace MarathonManager.Web.DTOs
{
    public class RaceDistanceCreateDto : IDistanceFormModel // <-- Implement interface
    {
        public int RaceId { get; set; }

        // Properties required by the interface (already defined with correct attributes)
        public string Name { get; set; }
        public decimal DistanceInKm { get; set; }
        public decimal RegistrationFee { get; set; }
        public int MaxParticipants { get; set; }
        public DateTime StartTime { get; set; }
    }
}