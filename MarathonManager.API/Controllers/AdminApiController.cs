using MarathonManager.API.DTOs; // Giả sử UserDto nằm ở đây
using MarathonManager.API.DTOs.Admin; // Namespace cho các DTO mới (RoleDto, UpdateUserRolesDto, BlogAdminDto)
using MarathonManager.API.DTOs.Race; // Cần cho RaceSummaryDto
using MarathonManager.API.DTOs.RaceDistances; // Cần cho RaceDistanceDto
using MarathonManager.API.Models; // Namespace cho User, BlogPost, MarathonManagerContext...
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Cần cho UserManager, RoleManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
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
        private readonly IWebHostEnvironment _environment;
        public AdminApiController(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            MarathonManagerContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _environment = environment;
        }

        // ===================================
        // QUẢN LÝ NGƯỜI DÙNG (USERS)
        // ===================================

        // GET: api/admin/users
        // Lấy tất cả user với đầy đủ thông tin
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var usersDto = await _userManager.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    IsActive = !u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTimeOffset.UtcNow,
                    CreatedAt = u.CreatedAt,
                    // Lấy các trường mở rộng
                    PhoneNumber = u.PhoneNumber,
                    EmailConfirmed = u.EmailConfirmed,
                    DateOfBirth = u.DateOfBirth, // Kiểu DateOnly?
                    Gender = u.Gender,           // Kiểu string?
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
        // Khóa hoặc Mở khóa tài khoản
        [HttpPatch("users/{id}/toggle-lock")]
        public async Task<IActionResult> ToggleUserLock(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            bool currentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            IdentityResult result;
            if (currentlyLocked)
                result = await _userManager.SetLockoutEndDateAsync(user, null); // Mở khóa
            else
                result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // Khóa

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

                // SỬA LỖI Ở DÒNG NÀY:
                IsActive = !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow, // Phải là "user", không phải "u"

                CreatedAt = user.CreatedAt,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Roles = roles.ToList()
            };
            return Ok(userDto);
        }

        // GET: api/admin/roles
        // Lấy tất cả các role
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
        // Cập nhật (thay đổi) role cho user
        [HttpPut("users/{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles(int id, [FromBody] UpdateUserRolesDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            dto.RoleNames ??= new List<string>(); // Đảm bảo không null

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
                foreach (var roleName in rolesToAdd)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        return BadRequest(new { message = $"Role '{roleName}' không tồn tại." });
                    }
                }
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
        // (ĐÃ SỬA MAPPING)
        [HttpGet("blogs")]
        public async Task<ActionResult<IEnumerable<BlogAdminDto>>> GetBlogPosts()
        {
            var blogs = await _context.BlogPosts
                .Include(b => b.Author) // **Quan trọng: Phải Include Author**
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BlogAdminDto
                {
                    Id = b.Id,
                    Title = b.Title, // **Sửa lỗi hiển thị Title**
                    Status = b.Status,
                    AuthorName = b.Author != null ? b.Author.FullName : "N/A", // **Sửa lỗi hiển thị Author**
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();
            return Ok(blogs);
        }

        // PATCH: api/admin/blogs/{id}/toggle-publish
        // Đăng/Gỡ bài
        [HttpPatch("blogs/{id}/toggle-publish")]
        public async Task<IActionResult> ToggleBlogPostPublish(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null)
            {
                return NotFound("Không tìm thấy bài viết.");
            }

            blogPost.Status = (blogPost.Status == "Published") ? "Draft" : "Published";
            blogPost.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Bài viết đã chuyển sang trạng thái {blogPost.Status}.", newStatus = blogPost.Status });
        }

        // DELETE: api/admin/blogs/{id}
        // Xóa bài
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

        // ===================================
        // QUẢN LÝ GIẢI CHẠY (RACES)
        // (CHỨC NĂNG MỚI)
        // ===================================

        // GET: api/admin/races/all
        // Lấy TẤT CẢ giải chạy (Pending, Approved, Cancelled)
        [HttpGet("races/all")]
        public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetAllRacesForAdmin()
        {
            var races = await _context.Races
                .Include(r => r.Organizer) // Include Organizer để lấy tên
                .OrderByDescending(r => r.RaceDate) // Sắp xếp theo ngày (mới nhất trước)
                .Select(r => new RaceSummaryDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Location = r.Location,
                    RaceDate = r.RaceDate,
                    ImageUrl = r.ImageUrl,
                    Status = r.Status,
                    OrganizerName = r.Organizer.FullName // Thêm tên người tổ chức
                })
                .ToListAsync();

            return Ok(races);
        }

        // GET: api/admin/races/detail/5
        // Admin xem chi tiết BẤT KỲ giải chạy nào
        [HttpGet("races/detail/{id}")]
        public async Task<ActionResult<RaceDetailDto>> GetRaceDetailsForAdmin(int id)
        {
            var race = await _context.Races
                .Include(r => r.Organizer)
                .Include(r => r.RaceDistances) // Lấy cả cự ly
                .Where(r => r.Id == id) // Không cần check Status
                .Select(r => new RaceDetailDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Location = r.Location,
                    RaceDate = r.RaceDate,
                    ImageUrl = r.ImageUrl,
                    Status = r.Status,
                    OrganizerName = r.Organizer.FullName,
                    Distances = r.RaceDistances.Select(d => new RaceDistanceDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        DistanceInKm = d.DistanceInKm,
                        RegistrationFee = d.RegistrationFee,
                        MaxParticipants = d.MaxParticipants,
                        StartTime = d.StartTime
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (race == null)
            {
                return NotFound(new { message = "Không tìm thấy giải chạy." });
            }
            return Ok(race);
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
            // An toàn hơn là throw exception nếu không lấy được ID
            throw new InvalidOperationException("Không thể xác định ID người dùng từ token.");
        }
        [HttpGet("blogs/{id}")]
        public async Task<ActionResult<BlogPost>> GetBlogPost(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);

            if (blogPost == null)
            {
                return NotFound();
            }

            // Admin có quyền xem mọi bài viết (kể cả Draft)
            return Ok(blogPost);
        }

        // ===================================
        // BLOG: TẠO BÀI VIẾT MỚI
        // POST: api/admin/blogs
        // ===================================
        [HttpPost("blogs")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateBlogPost([FromForm] BlogCreateDto blogDto, IFormFile? featuredImage)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string? imageUrlPath = null;

            // 1. Xử lý ảnh (giống hệt RacesController)
            if (featuredImage != null && featuredImage.Length > 0)
            {
                // (Bạn có thể thêm validation size, extension ở đây)
                try
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "images", "blogs");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(featuredImage.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await featuredImage.CopyToAsync(fileStream);
                    }
                    imageUrlPath = $"/images/blogs/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Lỗi khi lưu ảnh: {ex.Message}" });
                }
            }

            // 2. Tạo Blog Post
            var newPost = new BlogPost
            {
                Title = blogDto.Title,
                Content = blogDto.Content,
                Status = blogDto.Status,
                FeaturedImageUrl = imageUrlPath,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AuthorId = GetCurrentUserId() // Gán Author là Admin đang đăng nhập
            };

            _context.BlogPosts.Add(newPost);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBlogPost), new { id = newPost.Id }, newPost);
        }

        // ===================================
        // BLOG: CẬP NHẬT BÀI VIẾT
        // POST: api/admin/blogs/update (dùng POST thay vì PUT để [FromForm] dễ dàng)
        // ===================================
        [HttpPost("blogs/update")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateBlogPost([FromForm] BlogUpdateDto blogDto, IFormFile? featuredImage)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var postToUpdate = await _context.BlogPosts.FindAsync(blogDto.Id);
            if (postToUpdate == null)
            {
                return NotFound(new { message = "Không tìm thấy bài viết." });
            }

            // 1. Xử lý ảnh (nếu có ảnh mới)
            if (featuredImage != null && featuredImage.Length > 0)
            {
                // Xóa ảnh cũ (nếu có)
                if (!string.IsNullOrEmpty(postToUpdate.FeaturedImageUrl))
                {
                    try
                    {
                        string oldImagePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", postToUpdate.FeaturedImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    catch (Exception ex) { Console.WriteLine($"Lỗi xóa ảnh cũ: {ex.Message}"); }
                }

                // Lưu ảnh mới
                try
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "images", "blogs");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(featuredImage.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await featuredImage.CopyToAsync(fileStream);
                    }
                    postToUpdate.FeaturedImageUrl = $"/images/blogs/{uniqueFileName}"; // Cập nhật đường dẫn mới
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = $"Lỗi khi lưu ảnh mới: {ex.Message}" });
                }
            }

            // 2. Cập nhật thông tin
            postToUpdate.Title = blogDto.Title;
            postToUpdate.Content = blogDto.Content;
            postToUpdate.Status = blogDto.Status;
            postToUpdate.UpdatedAt = DateTime.UtcNow;

            _context.Entry(postToUpdate).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thành công." });
        }
    }
}