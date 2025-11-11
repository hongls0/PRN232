// Controllers/BlogsController.cs
using MarathonManager.API.DTOs;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace MarathonManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlogsController : ControllerBase
{
    private readonly MarathonManagerContext _context; // thay bằng DbContext của bạn

    public BlogsController(MarathonManagerContext context) => _context = context;

    private int GetCurrentUserId()
    {
        // giống hàm bạn đã dùng trong RacesController
        return int.Parse(User.Claims.First(c => c.Type.EndsWith("/nameidentifier")).Value);
    }

    // GET: api/Blogs?search=&page=1&pageSize=10
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<BlogListItemDto>>>> GetBlogs(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : (int?)null;

        var q = _context.BlogPosts
    .Include(b => b.Author)
    .Include(b => b.Comments)
    .Include(b => b.Likes)
    .Include(b => b.Race)            // thêm dòng này
    .Where(b => b.Status == "Published");

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(b => b.Title.Contains(search) || b.Content.Contains(search));

        var items = await q
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BlogListItemDto
            {
                Id = b.Id,
                Title = b.Title,
                Excerpt = b.Content.Length > 180 ? b.Content.Substring(0, 180) + "..." : b.Content,
                FeaturedImageUrl = b.FeaturedImageUrl,
                CreatedAt = b.CreatedAt,
                AuthorName = b.Author.FullName,
                LikeCount = b.Likes.Count(),
                CommentCount = b.Comments.Count(),
                IsLikedByCurrentUser = userId != null && b.Likes.Any(l => l.UserId == userId),

                RaceId = b.RaceId,
                RaceName = b.Race != null ? b.Race.Name : null
            })
            .ToListAsync();

        return Ok(new ApiResponse<List<BlogListItemDto>> { Success = true, Data = items });
    }

    // GET: api/Blogs/5
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<BlogDetailDto>>> GetBlog(int id)
    {
        var userId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : (int?)null;

        var b = await _context.BlogPosts
            .Include(x => x.Author)
            .Include(x => x.Comments).ThenInclude(c => c.User)
            .Include(x => x.Likes)
            .Include(x => x.Race)
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == "Published");

        if (b == null)
            return NotFound(new ApiResponse<BlogDetailDto> { Success = false, Message = "Blog not found" });

        var dto = new BlogDetailDto
        {
            Id = b.Id,
            Title = b.Title,
            Content = b.Content,
            FeaturedImageUrl = b.FeaturedImageUrl,
            CreatedAt = b.CreatedAt,
            AuthorName = b.Author.FullName,
            LikeCount = b.Likes.Count,
            CommentCount = b.Comments.Count,
            IsLikedByCurrentUser = userId != null && b.Likes.Any(l => l.UserId == userId),
            RaceId = b.RaceId,
            RaceName = b.Race?.Name,
            RaceDate = b.Race?.RaceDate,
            Comments = b.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentDto
                {
                    Id = c.Id,
                    BlogPostId = c.BlogPostId,
                    UserId = c.UserId,
                    UserName = c.User.FullName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToList()
        };

        return Ok(new ApiResponse<BlogDetailDto> { Success = true, Data = dto });
    }

    // POST: api/Blogs/5/like  (toggle like)
    [HttpPost("{id}/like")]
    [Authorize(Roles = "Runner")]
    public async Task<ActionResult<ApiResponse<ToggleLikeResponse>>> ToggleLike(int id)
    {
        var userId = GetCurrentUserId();

        var blog = await _context.BlogPosts
            .Include(b => b.Likes)
            .FirstOrDefaultAsync(b => b.Id == id && b.Status == "Published");

        if (blog == null)
            return NotFound(new ApiResponse<ToggleLikeResponse> { Success = false, Message = "Blog not found" });

        var like = await _context.Likes.FindAsync(userId, id); // khóa kép
        bool liked;

        if (like == null)
        {
            _context.Likes.Add(new Like
            {
                UserId = userId,
                BlogPostId = id,
                CreatedAt = DateTime.UtcNow
            });
            liked = true;
        }
        else
        {
            _context.Likes.Remove(like); // dislike
            liked = false;
        }

        await _context.SaveChangesAsync();

        var count = await _context.Likes.CountAsync(l => l.BlogPostId == id);

        return Ok(new ApiResponse<ToggleLikeResponse>
        {
            Success = true,
            Data = new ToggleLikeResponse { Liked = liked, LikeCount = count }
        });
    }

    // POST: api/Blogs/5/comments
    [HttpPost("{id}/comments")]
    [Authorize(Roles = "Runner")]
    public async Task<ActionResult<ApiResponse<CommentDto>>> CreateComment(int id, [FromBody] CreateCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(new ApiResponse<CommentDto> { Success = false, Message = "Content is required" });

        var userId = GetCurrentUserId();

        var blogExists = await _context.BlogPosts.AnyAsync(b => b.Id == id && b.Status == "Published");
        if (!blogExists)
            return NotFound(new ApiResponse<CommentDto> { Success = false, Message = "Blog not found" });

        var cmt = new Comment
        {
            BlogPostId = id,
            UserId = userId,
            Content = req.Content.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _context.Comments.Add(cmt);
        await _context.SaveChangesAsync();

        // load lại để có UserName
        var dto = await _context.Comments.Include(c => c.User)
            .Where(c => c.Id == cmt.Id)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                BlogPostId = c.BlogPostId,
                UserId = c.UserId,
                UserName = c.User.FullName,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            }).FirstAsync();

        return Ok(new ApiResponse<CommentDto> { Success = true, Data = dto });
    }

    // DELETE: api/Blogs/comments/123 (runner chỉ được xóa comment của chính mình)
    [HttpDelete("comments/{commentId}")]
    [Authorize(Roles = "Runner")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteComment(int commentId)
    {
        var userId = GetCurrentUserId();

        var c = await _context.Comments.FirstOrDefaultAsync(x => x.Id == commentId);
        if (c == null) return NotFound(new ApiResponse<object> { Success = false, Message = "Comment not found" });
        if (c.UserId != userId) return Forbid();

        _context.Comments.Remove(c);
        await _context.SaveChangesAsync();
        return Ok(new ApiResponse<object> { Success = true, Message = "Comment deleted" });
    }
}

