using System;
using System.Collections.Generic;

namespace MarathonManager.API.Models;

public partial class BlogPost
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? FeaturedImageUrl { get; set; }

    public string Status { get; set; } = null!;

    public int AuthorId { get; set; }

    public int? RaceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Author { get; set; } = null!;

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();

    public virtual Race? Race { get; set; }
}
