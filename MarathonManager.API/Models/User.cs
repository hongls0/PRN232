using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity; // Cần thêm dòng này

namespace MarathonManager.API.Models;

// 1. Kế thừa từ IdentityUser và chỉ định kiểu khóa chính là <int>
public partial class User : IdentityUser<int>
{
    // 2. Xóa các trường đã có trong IdentityUser:
    // public int Id { get; set; } // Đã có
    // public string Email { get; set; } = null!; // Đã có
    // public string PasswordHash { get; set; } = null!; // Đã có
    // public string? PhoneNumber { get; set; } // Đã có

    // 3. Giữ lại các trường tùy chỉnh (custom fields)
    public string FullName { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }

    // 4. Xóa trường Role (Identity sẽ quản lý việc này)
    // public string Role { get; set; } = null!;

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // 5. Giữ lại các liên kết (Navigation properties)
    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    public virtual ICollection<Race> Races { get; set; } = new List<Race>();
    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}