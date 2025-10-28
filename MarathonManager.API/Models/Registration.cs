using System;
using System.Collections.Generic;

namespace MarathonManager.API.Models;

public partial class Registration
{
    public int Id { get; set; }

    public int RunnerId { get; set; }

    public int RaceDistanceId { get; set; }

    public DateTime RegistrationDate { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string? BibNumber { get; set; }

    public virtual RaceDistance RaceDistance { get; set; } = null!;

    public virtual Result? Result { get; set; }

    public virtual User Runner { get; set; } = null!;
}
