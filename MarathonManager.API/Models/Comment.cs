using System;
using System.Collections.Generic;

namespace MarathonManager.API.Models;

public partial class Comment
{
    public int Id { get; set; }

    public int BlogPostId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual BlogPost BlogPost { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
