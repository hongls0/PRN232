using System;
using System.Collections.Generic;

namespace MarathonManager.API.Models;

public partial class Result
{
    public int Id { get; set; }

    public int RegistrationId { get; set; }

    public TimeOnly? CompletionTime { get; set; }

    public int? OverallRank { get; set; }

    public int? GenderRank { get; set; }

    public int? AgeCategoryRank { get; set; }

    public string Status { get; set; } = null!;

    public virtual Registration Registration { get; set; } = null!;
}
