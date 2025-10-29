using MarathonManager.Web.DTOs;
using MarathonManager.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;
using X.PagedList.Extensions;

namespace MarathonManager.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        // ===================================
        // SỬA LẠI HÀM INDEX ĐỂ THÊM BLOG
        // ===================================
        public async Task<IActionResult> Index(
            string tab = "races",
            int? pageRaces = 1,
            int? pageUsers = 1,
            int? pageBlogs = 1) // <-- Thêm pageBlogs
        {
            var client = CreateAuthenticatedHttpClient();
            var viewModel = new AdminDashboardViewModel();

            int pageSize = 10;
            int racesPageNumber = (pageRaces ?? 1);
            int usersPageNumber = (pageUsers ?? 1);
            int blogsPageNumber = (pageBlogs ?? 1); // <-- Thêm blogsPageNumber

            try
            {
                // 1. Lấy Races chờ duyệt
                var raceResponse = await client.GetAsync("/api/Races/pending");
                List<RaceSummaryDto> pendingRacesList = new List<RaceSummaryDto>();
                if (raceResponse.IsSuccessStatusCode)
                {
                    var json = await raceResponse.Content.ReadAsStringAsync();
                    pendingRacesList = JsonConvert.DeserializeObject<List<RaceSummaryDto>>(json);
                }
                viewModel.PendingRaces = pendingRacesList.ToPagedList(racesPageNumber, pageSize);

                // 2. Lấy Users
                var userResponse = await client.GetAsync("/api/admin/users");
                List<UserDto> allUsersList = new List<UserDto>();
                if (userResponse.IsSuccessStatusCode)
                {
                    var json = await userResponse.Content.ReadAsStringAsync();
                    allUsersList = JsonConvert.DeserializeObject<List<UserDto>>(json);
                }
                viewModel.AllUsers = allUsersList.ToPagedList(usersPageNumber, pageSize);

                // 3. Lấy Blog Posts <-- THÊM MỚI
                var blogResponse = await client.GetAsync("/api/admin/blogs");
                List<BlogAdminDto> allBlogsList = new List<BlogAdminDto>();
                if (blogResponse.IsSuccessStatusCode)
                {
                    var json = await blogResponse.Content.ReadAsStringAsync();
                    allBlogsList = JsonConvert.DeserializeObject<List<BlogAdminDto>>(json);
                }
                viewModel.AllBlogPosts = allBlogsList.ToPagedList(blogsPageNumber, pageSize); // <-- Phân trang Blog

            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi tải dữ liệu: " + ex.Message;
            }

            ViewBag.CurrentTab = tab;
            return View(viewModel);
        }

        // ... (Các hàm Approve, Cancel Race giữ nguyên) ...
        [HttpPost]
        public async Task<IActionResult> Approve(int raceId)
        {
            var client = CreateAuthenticatedHttpClient();
            await client.PatchAsync($"/api/Races/{raceId}/approve", null);
            return RedirectToAction("Index", new { tab = "races" });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int raceId)
        {
            var client = CreateAuthenticatedHttpClient();
            await client.PatchAsync($"/api/Races/{raceId}/cancel", null);
            return RedirectToAction("Index", new { tab = "races" });
        }

        // ... (Hàm ToggleUserLock giữ nguyên) ...
        [HttpPost]
        public async Task<IActionResult> ToggleUserLock(int userId)
        {
            var client = CreateAuthenticatedHttpClient();
            await client.PatchAsync($"/api/admin/users/{userId}/toggle-lock", null);
            return RedirectToAction("Index", new { tab = "users" });
        }

        // ===================================
        // HÀM MỚI: XỬ LÝ BLOG POSTS
        // ===================================
        [HttpPost]
        public async Task<IActionResult> ToggleBlogPostPublish(int blogId)
        {
            var client = CreateAuthenticatedHttpClient();
            await client.PatchAsync($"/api/admin/blogs/{blogId}/toggle-publish", null);
            return RedirectToAction("Index", new { tab = "blogs" }); // <-- Quay lại tab blogs
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBlogPost(int blogId)
        {
            var client = CreateAuthenticatedHttpClient();
            await client.DeleteAsync($"/api/admin/blogs/{blogId}");
            return RedirectToAction("Index", new { tab = "blogs" }); // <-- Quay lại tab blogs
        }

        // ... (Hàm CreateAuthenticatedHttpClient giữ nguyên) ...
        private HttpClient CreateAuthenticatedHttpClient()
        {
            var client = _httpClientFactory.CreateClient("MarathonApi");
            var token = _httpContextAccessor.HttpContext.Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Không tìm thấy Token. Vui lòng đăng nhập lại.");
            }
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}