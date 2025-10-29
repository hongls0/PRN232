using MarathonManager.API.DTOs; // Giả sử UserDto nằm ở đây
using MarathonManager.API.DTOs.Admin; // Namespace cho các DTO mới (RoleDto, UpdateUserRolesDto, BlogAdminDto)
using MarathonManager.API.Models; // Namespace cho User, BlogPost, MarathonManagerContext...
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Cần cho UserManager, RoleManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // Cần cho DateTimeOffset, DateOnly
using System.Collections.Generic; // Cần cho List
using System.Linq;              // Cần cho LINQ (ToList, Select...)
using System.Security.Claims;   // Cần cho GetCurrentUserId
using System.Threading.Tasks;   // Cần cho async/await

namespace MarathonManager.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Khóa toàn bộ, chỉ Admin
    public class AdminApiController : ControllerBase
    {
        // Quan trọng: Đảm bảo 'User' là class kế thừa IdentityUser<int> CỦA BẠN
        // và CÓ chứa các thuộc tính DateOfBirth (DateOnly?), Gender (string?)
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly MarathonManagerContext _context;

        public AdminApiController(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            MarathonManagerContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // ===================================
        // QUẢN LÝ NGƯỜI DÙNG (USERS)
        // ===================================

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            // Sử dụng _userManager.Users thường an toàn hơn để truy cập dữ liệu Identity
            // Hoặc đảm bảo _context.Users được cấu hình đúng để trỏ đến AspNetUsers
            var usersDto = await _userManager.Users // Hoặc _context.Users nếu bạn chắc chắn
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    IsActive = !u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTimeOffset.UtcNow,
                    CreatedAt = u.CreatedAt,
                    // Lấy các trường mới (Đảm bảo model 'User' có các thuộc tính này)
                    PhoneNumber = u.PhoneNumber,
                    EmailConfirmed = u.EmailConfirmed,
                    DateOfBirth = u.DateOfBirth, // Kiểu DateOnly?
                    Gender = u.Gender,           // Kiểu string?
                    // Lấy tên Role (Cách này ổn)
                    Roles = (from ur in _context.UserRoles
                             join r in _context.Roles on ur.RoleId equals r.Id
                             where ur.UserId == u.Id
                             select r.Name).ToList() ?? new List<string>()
                })
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return Ok(usersDto);
        }

        // PATCH: api/admin/users/{id}/toggle-lock
        [HttpPatch("users/{id}/toggle-lock")]
        public async Task<IActionResult> ToggleUserLock(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            bool currentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            IdentityResult result;
            if (currentlyLocked) result = await _userManager.SetLockoutEndDateAsync(user, null); // Mở khóa
            else result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // Khóa

            if (result.Succeeded)
            {
                bool isActiveNow = !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow;
                return Ok(new { message = $"Tài khoản đã {(isActiveNow ? "mở khóa" : "bị khóa")}.", isActive = isActiveNow });
            }
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest($"Cập nhật thất bại: {errors}");
        }

        // GET: api/admin/users/5
        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow,
                CreatedAt = user.CreatedAt,
                // Lấy các trường mới (Đảm bảo model 'User' có các thuộc tính này)
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                DateOfBirth = user.DateOfBirth, // Kiểu DateOnly?
                Gender = user.Gender,           // Kiểu string?
                Roles = roles.ToList()
            };
            return Ok(userDto);
        }

        // GET: api/admin/roles
        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
        {
            var roles = await _roleManager.Roles
                .Select(r => new RoleDto { Id = r.Id.ToString(), Name = r.Name })
                .OrderBy(r => r.Name)
                .ToListAsync();
            return Ok(roles);
        }

        // PUT: api/admin/users/5/roles
        [HttpPut("users/{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles(int id, [FromBody] UpdateUserRolesDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            dto.RoleNames ??= new List<string>();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(dto.RoleNames).ToList();
            var rolesToAdd = dto.RoleNames.Except(currentRoles).ToList();

            // Xử lý xóa role
            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Lỗi khi xóa role cũ: {errors}" });
                }
            }

            // Xử lý thêm role
            if (rolesToAdd.Any())
            {
                // Kiểm tra role tồn tại
                foreach (var roleName in rolesToAdd)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        return BadRequest(new { message = $"Role '{roleName}' không tồn tại." });
                    }
                }
                // Thêm role
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Lỗi khi thêm role mới: {errors}" });
                }
            }

            return Ok(new { message = "Cập nhật role thành công." });
        }

        // ===================================
        // QUẢN LÝ BLOG POSTS
        // ===================================

        // GET: api/admin/blogs
        [HttpGet("blogs")]
        public async Task<ActionResult<IEnumerable<BlogAdminDto>>> GetBlogPosts()
        {
            var blogs = await _context.BlogPosts
                .Include(b => b.Author) // **Quan trọng: Phải Include Author**
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BlogAdminDto
                {
                    Id = b.Id,
                    Title = b.Title, // **Đã kiểm tra**
                    Status = b.Status,
                    AuthorName = b.Author != null ? b.Author.FullName : "N/A", // **Đã kiểm tra**
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();
            return Ok(blogs); // **Đã kiểm tra return**
        }

        // PATCH: api/admin/blogs/{id}/toggle-publish
        [HttpPatch("blogs/{id}/toggle-publish")]
        public async Task<IActionResult> ToggleBlogPostPublish(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null)
            {
                return NotFound("Không tìm thấy bài viết."); // **Đã kiểm tra return**
            }

            blogPost.Status = (blogPost.Status == "Published") ? "Draft" : "Published";
            blogPost.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Bài viết đã chuyển sang trạng thái {blogPost.Status}.", newStatus = blogPost.Status }); // **Đã kiểm tra return**
        }

        // DELETE: api/admin/blogs/{id}
        [HttpDelete("blogs/{id}")]
        public async Task<IActionResult> DeleteBlogPost(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null)
            {
                return NotFound("Không tìm thấy bài viết."); // **Đã kiểm tra return**
            }

            _context.BlogPosts.Remove(blogPost);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa bài viết thành công." }); // **Đã kiểm tra return**
        }

        // ===================================
        // HÀM PHỤ TRỢ (Private)
        // ===================================
        private int GetCurrentUserId()
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            {
                return userId;
            }
            // Cân nhắc throw exception nếu ID không hợp lệ là nghiêm trọng
            // throw new InvalidOperationException("Không thể xác định ID người dùng.");
            return 0; // Hoặc trả về 0 nếu có thể chấp nhận
        }
    }
}