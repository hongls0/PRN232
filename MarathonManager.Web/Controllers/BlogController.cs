// Controllers/BlogController.cs (Web MVC)
using MarathonManager.API.DTOs;
using MarathonManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class BlogController : Controller
{
    private readonly IRunnerApiService _api;
    private readonly ILogger<BlogController> _logger;

    public BlogController(IRunnerApiService api, ILogger<BlogController> logger)
    {
        _api = api; _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var res = await _api.GetBlogsAsync(search, page, 10);
        if (!res.Success) TempData["ErrorMessage"] = res.Message;
        return View(res.Data ?? new List<BlogListItemDto>());
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Detail(int id)
    {
        var res = await _api.GetBlogAsync(id);
        if (!res.Success || res.Data == null)
        {
            TempData["ErrorMessage"] = res.Message ?? "Blog not found";
            return RedirectToAction(nameof(Index));
        }
        return View(res.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Runner")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var res = await _api.ToggleLikeAsync(id);
        if (!res.Success) TempData["ErrorMessage"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Runner")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateComment(int id, string content)
    {
        var res = await _api.CreateCommentAsync(id, content);
        if (!res.Success) TempData["ErrorMessage"] = res.Message;
        else TempData["SuccessMessage"] = "Comment added";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [Authorize(Roles = "Runner")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int id, int commentId)
    {
        var res = await _api.DeleteCommentAsync(commentId);
        if (!res.Success) TempData["ErrorMessage"] = res.Message;
        else TempData["SuccessMessage"] = "Comment deleted";
        return RedirectToAction(nameof(Detail), new { id });
    }
}

