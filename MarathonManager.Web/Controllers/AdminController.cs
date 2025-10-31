using MarathonManager.Web.DTOs;

using MarathonManager.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using X.PagedList;
using X.PagedList.Extensions;
// X.PagedList.Extensions không cần thiết nếu bạn dùng .ToPagedList()

namespace MarathonManager.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public AdminController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration; // Sửa lỗi: Bỏ 1 dòng lặp lại
        }

        // ===================================
        // INDEX (DASHBOARD)
        // ===================================
        public async Task<IActionResult> Index(
            string tab = "races",
            int? pageRaces = 1,
            int? pageUsers = 1,
            int? pageBlogs = 1)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            // Dùng tên ViewModel của bạn (AdminUserDetailViewModel là cho trang chi tiết)
            // Tên ViewModel cho trang Index phải là AdminDashboardViewModel
            var viewModel = new AdminDashboardViewModel();

            int pageSize = 10;
            int racesPageNumber = (pageRaces ?? 1);
            int usersPageNumber = (pageUsers ?? 1);
            int blogsPageNumber = (pageBlogs ?? 1);

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
                viewModel.AllRaces = allRacesList.ToPagedList(racesPageNumber, pageSize);

                // 2. Lấy Users
                var userResponse = await client.GetAsync("/api/admin/users");
                List<UserDto> allUsersList = new List<UserDto>();
                if (userResponse.IsSuccessStatusCode)
                {
                    var json = await userResponse.Content.ReadAsStringAsync();
                    allUsersList = JsonConvert.DeserializeObject<List<UserDto>>(json) ?? new List<UserDto>();
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

        // GET: /Admin/RaceDetails/5
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
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Không tải được chi tiết giải chạy: {error}";
                    return RedirectToAction("Index", new { tab = "races" });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi kết nối: " + ex.Message;
                return RedirectToAction("Index", new { tab = "races" });
            }

            if (raceDetail == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy giải chạy.";
                return RedirectToAction("Index", new { tab = "races" });
            }

            // Truyền ApiBaseUrl sang View để hiển thị ảnh
            ViewBag.ApiBaseUrl = _configuration["ApiBaseUrl"];
            return View(raceDetail);
        }

        // POST: /Admin/Approve
        [HttpPost]
        public async Task<IActionResult> Approve(int raceId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var response = await client.PatchAsync($"/api/Races/{raceId}/approve", null);

            // Thêm TempData
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Duyệt giải chạy thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Duyệt giải chạy thất bại.";
            }

            return RedirectToAction("Index", new { tab = "races" });
        }

        // POST: /Admin/Cancel
        [HttpPost]
        public async Task<IActionResult> Cancel(int raceId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var response = await client.PatchAsync($"/api/Races/{raceId}/cancel", null);

            // Thêm TempData
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đã hủy giải chạy.";
            }
            else
            {
                TempData["ErrorMessage"] = "Hủy giải chạy thất bại.";
            }
            return RedirectToAction("Index", new { tab = "races" });
        }

        // ===================================
        // QUẢN LÝ NGƯỜI DÙNG (USERS)
        // ===================================

        // POST: /Admin/ToggleUserLock
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
                    // Lấy message từ API (VD: "Tài khoản đã bị khóa")
                    TempData["SuccessMessage"] = (string)(resultObj?.message) ?? "Thay đổi trạng thái tài khoản thành công.";
                }
                catch { TempData["SuccessMessage"] = "Thay đổi trạng thái tài khoản thành công."; }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = "Lỗi: " + error;
            }

            return RedirectToAction("Index", new { tab = "users" });
        }

        // GET: /Admin/UserDetails?userId=5
        public async Task<IActionResult> UserDetails(int userId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            // Dùng ViewModel bạn đã tạo
            var viewModel = new AdminUserDetailViewModel();

            try
            {
                // 1. Lấy thông tin chi tiết user
                var userResponse = await client.GetAsync($"/api/admin/users/{userId}");
                if (userResponse.IsSuccessStatusCode)
                {
                    var json = await userResponse.Content.ReadAsStringAsync();
                    viewModel.User = JsonConvert.DeserializeObject<UserDto>(json);
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tải được thông tin User.";
                    return RedirectToAction("Index", new { tab = "users" });
                }

                // 2. Lấy tất cả các role có sẵn
                var rolesResponse = await client.GetAsync("/api/admin/roles");
                if (rolesResponse.IsSuccessStatusCode)
                {
                    var json = await rolesResponse.Content.ReadAsStringAsync();
                    viewModel.AllRoles = JsonConvert.DeserializeObject<List<RoleDto>>(json) ?? new List<RoleDto>();
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi kết nối API: " + ex.Message;
                return RedirectToAction("Index", new { tab = "users" });
            }

            if (viewModel.User == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy User.";
                return RedirectToAction("Index", new { tab = "users" });
            }

            return View(viewModel); // Trả về View "UserDetails.cshtml"
        }

        // POST: /Admin/UpdateUserRoles
        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm AntiForgeryToken
        public async Task<IActionResult> UpdateUserRoles(int userId, List<string> selectedRoles)
        {
            if (userId <= 0) return BadRequest("User ID không hợp lệ.");
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var dto = new UpdateUserRolesDto { RoleNames = selectedRoles ?? new List<string>() };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PutAsync($"/api/admin/users/{userId}/roles", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật vai trò thành công!";
                }
                else
                {
                    string errorMsg = "Cập nhật thất bại.";
                    try
                    {
                        var errorJson = await response.Content.ReadAsStringAsync();
                        var errorObj = JsonConvert.DeserializeObject<dynamic>(errorJson);
                        errorMsg += $" Lý do: {errorObj?.message ?? errorJson}";
                    }
                    catch { /* Bỏ qua nếu không parse được lỗi */ }
                    TempData["ErrorMessage"] = errorMsg;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi kết nối API: " + ex.Message;
            }

            return RedirectToAction("UserDetails", new { userId = userId });
        }

        // ===================================
        // QUẢN LÝ BLOG (BLOGS)
        // ===================================
        [HttpPost]
        public async Task<IActionResult> ToggleBlogPostPublish(int blogId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var response = await client.PatchAsync($"/api/admin/blogs/{blogId}/toggle-publish", null);

            // Thêm TempData
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
            else
            {
                TempData["ErrorMessage"] = "Thay đổi trạng thái thất bại.";
            }

            return RedirectToAction("Index", new { tab = "blogs" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBlogPost(int blogId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var response = await client.DeleteAsync($"/api/admin/blogs/{blogId}");

            // Thêm TempData
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Xóa bài viết thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Xóa bài viết thất bại.";
            }

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
                Console.WriteLine("AuthToken cookie not found."); // Log lỗi
                return null;
            }

            client.DefaultRequestHeaders.Authorization = null;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
    }
}