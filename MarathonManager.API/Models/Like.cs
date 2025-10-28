using System;
using System.Collections.Generic;

namespace MarathonManager.API.Models;

public partial class Like
{
    public int UserId { get; set; }

    public int BlogPostId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual BlogPost BlogPost { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
