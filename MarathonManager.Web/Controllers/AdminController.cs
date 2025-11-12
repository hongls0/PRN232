using MarathonManager.Web.DTOs;
using MarathonManager.Web.DTOs.Admin; // Cần cho BlogAdminDto, RoleDto, UpdateUserRolesDto
using MarathonManager.Web.ViewModels; // Cần cho AdminDashboardViewModel, AdminUserDetailViewModel
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using X.PagedList; // <-- DÒNG NÀY SỬA LỖI 'ToPagedList'
using X.PagedList.Extensions;

namespace MarathonManager.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment; // Cần cho upload file

        public AdminController(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _environment = environment;
        }

        // ===================================
        // INDEX (DASHBOARD) - CÓ SEARCH
        // ===================================
        public async Task<IActionResult> Index(
            string tab = "races",
            int? pageRaces = 1,
            int? pageUsers = 1,
            int? pageBlogs = 1,
            string searchRaces = "",
            string searchUsers = "",
            string searchBlogs = "")
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var viewModel = new AdminDashboardViewModel();

            int pageSize = 10;
            int racesPageNumber = (pageRaces ?? 1);
            int usersPageNumber = (pageUsers ?? 1);
            int blogsPageNumber = (pageBlogs ?? 1);

            // Lưu giá trị search vào ViewBag để hiển thị lại trong form
            ViewBag.SearchRaces = searchRaces;
            ViewBag.SearchUsers = searchUsers;
            ViewBag.SearchBlogs = searchBlogs;

            try
            {
                // 1. Lấy TẤT CẢ Races
                var raceResponse = await client.GetAsync("/api/admin/races/all");
                List<RaceSummaryDto> allRacesList = new List<RaceSummaryDto>();
                if (raceResponse.IsSuccessStatusCode)
                {
                    var json = await raceResponse.Content.ReadAsStringAsync();
                    allRacesList = JsonConvert.DeserializeObject<List<RaceSummaryDto>>(json) ?? new List<RaceSummaryDto>();
                }

                // Lọc theo search (client-side)
                if (!string.IsNullOrWhiteSpace(searchRaces))
                {
                    var searchLower = searchRaces.ToLower();
                    allRacesList = allRacesList.Where(r =>
                        (r.Name != null && r.Name.ToLower().Contains(searchLower)) ||
                        (r.OrganizerName != null && r.OrganizerName.ToLower().Contains(searchLower)) ||
                        (r.Location != null && r.Location.ToLower().Contains(searchLower))
                    ).ToList();
                }
                viewModel.AllRaces = allRacesList.ToPagedList(racesPageNumber, pageSize);

                // 2. Lấy Users
                var userResponse = await client.GetAsync("/api/admin/users");
                List<UserDto> allUsersList = new List<UserDto>();
                if (userResponse.IsSuccessStatusCode)
                {
                    var json = await userResponse.Content.ReadAsStringAsync();
                    allUsersList = JsonConvert.DeserializeObject<List<UserDto>>(json) ?? new List<UserDto>();
                }

                // Lọc theo search (client-side)
                if (!string.IsNullOrWhiteSpace(searchUsers))
                {
                    var searchLower = searchUsers.ToLower();
                    allUsersList = allUsersList.Where(u =>
                        (u.FullName != null && u.FullName.ToLower().Contains(searchLower)) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchLower))
                    ).ToList();
                }
                viewModel.AllUsers = allUsersList.ToPagedList(usersPageNumber, pageSize);

                // 3. Lấy Blog Posts
                var blogResponse = await client.GetAsync("/api/admin/blogs");
                List<BlogAdminDto> allBlogsList = new List<BlogAdminDto>();
                if (blogResponse.IsSuccessStatusCode)
                {
                    var json = await blogResponse.Content.ReadAsStringAsync();
                    allBlogsList = JsonConvert.DeserializeObject<List<BlogAdminDto>>(json) ?? new List<BlogAdminDto>();
                }

                // Lọc theo search (client-side)
                if (!string.IsNullOrWhiteSpace(searchBlogs))
                {
                    var searchLower = searchBlogs.ToLower();
                    allBlogsList = allBlogsList.Where(b =>
                        (b.Title != null && b.Title.ToLower().Contains(searchLower)) ||
                        (b.AuthorName != null && b.AuthorName.ToLower().Contains(searchLower))
                    ).ToList();
                }
                viewModel.AllBlogPosts = allBlogsList.ToPagedList(blogsPageNumber, pageSize);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi tải dữ liệu: " + ex.Message;
            }

            ViewBag.CurrentTab = tab;
            return View(viewModel);
        }

        // ===================================
        // QUẢN LÝ GIẢI CHẠY (RACES)
        // ===================================
        public async Task<IActionResult> RaceDetails(int id)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            RaceDetailDto raceDetail = null;
            try
            {
                var response = await client.GetAsync($"/api/admin/races/detail/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    raceDetail = JsonConvert.DeserializeObject<RaceDetailDto>(json);
                }
                else { /*... Xử lý lỗi ...*/ return RedirectToAction("Index", new { tab = "races" }); }
            }
            catch { /*... Xử lý lỗi ...*/ return RedirectToAction("Index", new { tab = "races" }); }
            if (raceDetail == null) { /*... Xử lý lỗi ...*/ return RedirectToAction("Index", new { tab = "races" }); }

            ViewBag.ApiBaseUrl = _configuration["ApiBaseUrl"];
            return View(raceDetail);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int raceId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            var response = await client.PatchAsync($"/api/Races/{raceId}/approve", null);
            if (response.IsSuccessStatusCode) TempData["SuccessMessage"] = "Duyệt giải chạy thành công!";
            else TempData["ErrorMessage"] = "Duyệt giải chạy thất bại.";
            return RedirectToAction("Index", new { tab = "races" });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int raceId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            var response = await client.PatchAsync($"/api/Races/{raceId}/cancel", null);
            if (response.IsSuccessStatusCode) TempData["SuccessMessage"] = "Đã hủy giải chạy.";
            else TempData["ErrorMessage"] = "Hủy giải chạy thất bại.";
            return RedirectToAction("Index", new { tab = "races" });
        }

        // ===================================
        // QUẢN LÝ NGƯỜI DÙNG (USERS)
        // ===================================
        [HttpPost]
        public async Task<IActionResult> ToggleUserLock(int userId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            var response = await client.PatchAsync($"/api/admin/users/{userId}/toggle-lock", null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jsonResult = await response.Content.ReadAsStringAsync();
                    var resultObj = JsonConvert.DeserializeObject<dynamic>(jsonResult);
                    TempData["SuccessMessage"] = (string)(resultObj?.message) ?? "Thay đổi trạng thái tài khoản thành công.";
                }
                catch { TempData["SuccessMessage"] = "Thay đổi trạng thái tài khoản thành công."; }
            }
            else { TempData["ErrorMessage"] = "Lỗi khi thay đổi trạng thái."; }
            return RedirectToAction("Index", new { tab = "users" });
        }

        public async Task<IActionResult> UserDetails(int userId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            var viewModel = new AdminUserDetailViewModel();
            try
            {
                var userResponse = await client.GetAsync($"/api/admin/users/{userId}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var json = await userResponse.Content.ReadAsStringAsync();
                    viewModel.User = JsonConvert.DeserializeObject<UserDto>(json);
                }
                else { /*... Xử lý lỗi ...*/ return RedirectToAction("Index", new { tab = "users" }); }

                var rolesResponse = await client.GetAsync("/api/admin/roles");
                if (rolesResponse.IsSuccessStatusCode)
                {
                    var json = await rolesResponse.Content.ReadAsStringAsync();
                    viewModel.AllRoles = JsonConvert.DeserializeObject<List<RoleDto>>(json) ?? new List<RoleDto>();
                }
            }
            catch { /*... Xử lý lỗi ...*/ return RedirectToAction("Index", new { tab = "users" }); }
            if (viewModel.User == null) { /*... Xử lý lỗi ...*/ return RedirectToAction("Index", new { tab = "users" }); }
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRoles(int userId, string selectedRole)
        {
            if (userId <= 0) return BadRequest("User ID không hợp lệ.");
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var rolesList = new List<string>();
            if (!string.IsNullOrEmpty(selectedRole)) rolesList.Add(selectedRole);
            var dto = new UpdateUserRolesDto { RoleNames = rolesList };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PutAsync($"/api/admin/users/{userId}/roles", jsonContent);
                if (response.IsSuccessStatusCode) TempData["SuccessMessage"] = "Cập nhật vai trò thành công!";
                else
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(errorJson);
                    TempData["ErrorMessage"] = $"Lỗi khi cập nhật Role: {errorObj?.message ?? errorJson}";
                }
            }
            catch (Exception ex) { TempData["ErrorMessage"] = "Lỗi kết nối API: " + ex.Message; }
            return RedirectToAction("UserDetails", new { userId = userId });
        }

        // ===================================
        // QUẢN LÝ BLOG (BLOGS)
        // ===================================

        // GET: /Admin/CreateBlog
        public IActionResult CreateBlog()
        {
            var model = new BlogCreateDto();
            return View(model);
        }

        // POST: /Admin/CreateBlog
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBlog(BlogCreateDto dto, IFormFile? featuredImage)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(dto.Title ?? ""), nameof(dto.Title));
            content.Add(new StringContent(dto.Content ?? ""), nameof(dto.Content));
            content.Add(new StringContent(dto.Status ?? "Draft"), nameof(dto.Status));

            if (featuredImage != null && featuredImage.Length > 0)
            {
                var streamContent = new StreamContent(featuredImage.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(featuredImage.ContentType);
                content.Add(streamContent, "featuredImage", featuredImage.FileName);
            }

            try
            {
                var response = await client.PostAsync("/api/admin/blogs", content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Tạo bài viết mới thành công!";
                    return RedirectToAction("Index", new { tab = "blogs" });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Lỗi API: {error}");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi kết nối: {ex.Message}");
            }
            return View(dto);
        }

        // GET: /Admin/EditBlog/5
        public async Task<IActionResult> EditBlog(int id)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            try
            {
                var response = await client.GetAsync($"/api/admin/blogs/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var blogPost = JsonConvert.DeserializeObject<BlogUpdateDto>(json);
                    if (blogPost == null) throw new Exception("Không thể đọc dữ liệu bài viết.");

                    var dynamicPost = JsonConvert.DeserializeObject<dynamic>(json);
                    blogPost.CurrentImageUrl = dynamicPost.featuredImageUrl;

                    ViewBag.ApiBaseUrl = _configuration["ApiBaseUrl"];
                    return View(blogPost);
                }
                else { /*...*/ TempData["ErrorMessage"] = "Không tìm thấy bài viết."; }
            }
            catch (Exception ex) { TempData["ErrorMessage"] = $"Lỗi kết nối: {ex.Message}"; }
            return RedirectToAction("Index", new { tab = "blogs" });
        }

        // POST: /Admin/EditBlog/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBlog(BlogUpdateDto dto, IFormFile? featuredImage)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ApiBaseUrl = _configuration["ApiBaseUrl"];
                return View(dto);
            }
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(dto.Id.ToString()), nameof(dto.Id));
            content.Add(new StringContent(dto.Title ?? ""), nameof(dto.Title));
            content.Add(new StringContent(dto.Content ?? ""), nameof(dto.Content));
            content.Add(new StringContent(dto.Status ?? "Draft"), nameof(dto.Status));

            if (featuredImage != null && featuredImage.Length > 0)
            {
                var streamContent = new StreamContent(featuredImage.OpenReadStream());
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(featuredImage.ContentType);
                content.Add(streamContent, "featuredImage", featuredImage.FileName);
            }

            try
            {
                var response = await client.PostAsync("/api/admin/blogs/update", content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                    return RedirectToAction("Index", new { tab = "blogs" });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Lỗi API: {error}");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi kết nối: {ex.Message}");
            }
            ViewBag.ApiBaseUrl = _configuration["ApiBaseUrl"];
            return View(dto);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleBlogPostPublish(int blogId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            var response = await client.PatchAsync($"/api/admin/blogs/{blogId}/toggle-publish", null);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var jsonResult = await response.Content.ReadAsStringAsync();
                    var resultObj = JsonConvert.DeserializeObject<dynamic>(jsonResult);
                    TempData["SuccessMessage"] = (string)(resultObj?.message) ?? "Thay đổi trạng thái bài viết thành công.";
                }
                catch { TempData["SuccessMessage"] = "Thay đổi trạng thái bài viết thành công."; }
            }
            else { TempData["ErrorMessage"] = "Thay đổi trạng thái thất bại."; }
            return RedirectToAction("Index", new { tab = "blogs" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBlogPost(int blogId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            var response = await client.DeleteAsync($"/api/admin/blogs/{blogId}");
            if (response.IsSuccessStatusCode) TempData["SuccessMessage"] = "Xóa bài viết thành công.";
            else TempData["ErrorMessage"] = "Xóa bài viết thất bại.";
            return RedirectToAction("Index", new { tab = "blogs" });
        }

        // ===================================
        // HÀM PHỤ TRỢ (Private)
        // ===================================
        private HttpClient CreateAuthenticatedHttpClient()
        {
            var client = _httpClientFactory.CreateClient("MarathonApi");
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("AuthToken cookie not found.");
                return null;
            }

            client.DefaultRequestHeaders.Authorization = null;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}