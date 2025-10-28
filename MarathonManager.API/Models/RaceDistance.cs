using System;
using System.Collections.Generic;

namespace MarathonManager.API.Models;

public partial class RaceDistance
{
    public int Id { get; set; }

    public int RaceId { get; set; }

    public string Name { get; set; } = null!;

    public decimal DistanceInKm { get; set; }

    public decimal RegistrationFee { get; set; }

    public int MaxParticipants { get; set; }

    public DateTime StartTime { get; set; }

    public virtual Race Race { get; set; } = null!;

    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
