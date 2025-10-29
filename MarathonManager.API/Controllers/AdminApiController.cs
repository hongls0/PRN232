using MarathonManager.API.DTOs.Admin;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarathonManager.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Khóa toàn bộ, chỉ Admin
    public class AdminApiController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly MarathonManagerContext _context;

        public AdminApiController(UserManager<User> userManager, MarathonManagerContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: api/admin/users
        // Lấy tất cả người dùng (Runners và Organizers)
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _userManager.Users
                .Where(u => u.Id != GetCurrentUserId()) // Không lấy chính Admin
                .ToListAsync();

            var userDtoList = new List<UserDto>();

            foreach (var user in users)
            {
                userDtoList.Add(new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList()
                });
            }

            return Ok(userDtoList);
        }
        // ... (using statements và constructor giữ nguyên) ...

        // GET: api/admin/blogs
        // Lấy tất cả bài blog (sắp xếp mới nhất lên đầu)
        [HttpGet("blogs")]
        public async Task<ActionResult<IEnumerable<BlogAdminDto>>> GetBlogPosts()
        {
            var blogs = await _context.BlogPosts
                .Include(b => b.Author) // Lấy thông tin Author để lấy tên
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BlogAdminDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Status = b.Status,
                    AuthorName = b.Author.FullName, // Lấy tên từ Author liên kết
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();

            return Ok(blogs);
        }

        // PATCH: api/admin/blogs/{id}/toggle-publish
        // Chuyển đổi trạng thái Published <-> Draft
        [HttpPatch("blogs/{id}/toggle-publish")]
        public async Task<IActionResult> ToggleBlogPostPublish(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null)
            {
                return NotFound("Không tìm thấy bài viết.");
            }

            // Đảo ngược trạng thái
            blogPost.Status = (blogPost.Status == "Published") ? "Draft" : "Published";
            blogPost.UpdatedAt = DateTime.UtcNow; // Cập nhật thời gian sửa

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Bài viết đã chuyển sang trạng thái {blogPost.Status}.",
                newStatus = blogPost.Status
            });
        }

        // DELETE: api/admin/blogs/{id}
        // Xóa bài viết
        [HttpDelete("blogs/{id}")]
        public async Task<IActionResult> DeleteBlogPost(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null)
            {
                return NotFound("Không tìm thấy bài viết.");
            }

            _context.BlogPosts.Remove(blogPost);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa bài viết thành công." });
        }

        // ... (Các hàm GetUsers, ToggleUserLock, GetCurrentUserId giữ nguyên) ...
        // PATCH: api/admin/users/{id}/toggle-lock
        // Khóa hoặc Mở khóa tài khoản
        [HttpPatch("users/{id}/toggle-lock")]
        public async Task<IActionResult> ToggleUserLock(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            user.IsActive = !user.IsActive; // Đảo ngược trạng thái
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new
                {
                    message = $"Tài khoản đã {(user.IsActive ? "mở khóa" : "bị khóa")}.",
                    isActive = user.IsActive
                });
            }

            return BadRequest("Cập nhật thất bại.");
        }

        // Bạn cũng có thể thêm API lấy danh sách Blog (pending/published) ở đây
        // GET: api/admin/blogs
        // ...

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
        }
    }
}