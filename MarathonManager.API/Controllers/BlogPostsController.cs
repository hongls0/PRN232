using MarathonManager.API.DTOs.Blog;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogPostsController : ControllerBase
    {
        private readonly MarathonManagerContext _context;

        public BlogPostsController(MarathonManagerContext context)
        {
            _context = context;
        }

        // GET: api/BlogPosts
        // Lấy 3 bài blog/event mới nhất đã được "Published"
        [HttpGet]
        [AllowAnonymous] // Cho phép xem công khai
        public async Task<ActionResult<IEnumerable<BlogSummaryDto>>> GetBlogPosts()
        {
            var posts = await _context.BlogPosts
                .Where(p => p.Status == "Published") // Chỉ lấy bài đã đăng
                .OrderByDescending(p => p.CreatedAt) // Mới nhất lên đầu
                .Take(3) // Chỉ lấy 3 bài
                .Select(p => new BlogSummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    FeaturedImageUrl = p.FeaturedImageUrl,
                    CreatedAt = p.CreatedAt,
                    // Cắt chuỗi Content để lấy tóm tắt (ví dụ: 100 ký tự)
                    Summary = p.Content.Length > 100
                              ? p.Content.Substring(0, 100) + "..."
                              : p.Content
                })
                .ToListAsync();

            return Ok(posts);
        }

        // (Bạn có thể thêm các hàm GET(id), POST, PUT... sau)
    }
}