using System;
using System.Collections.Generic;

namespace MarathonManager.API.Models;

public partial class Race
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Location { get; set; } = null!;

    public DateTime RaceDate { get; set; }

    public string? ImageUrl { get; set; }

    public int OrganizerId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();

    public virtual User Organizer { get; set; } = null!;

    public virtual ICollection<RaceDistance> RaceDistances { get; set; } = new List<RaceDistance>();
}
