using MarathonManager.Web.DTOs;
using MarathonManager.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json; // <-- Đảm bảo có using này
using System.Collections.Generic;
using System.Net.Http;
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
        // INDEX (DASHBOARD) - Sửa cách đọc response
        // ===================================
        public async Task<IActionResult> Index(
            string tab = "races",
            int? pageRaces = 1,
            int? pageUsers = 1,
            int? pageBlogs = 1)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var viewModel = new AdminDashboardViewModel();
            int pageSize = 10;
            int racesPageNumber = (pageRaces ?? 1);
            int usersPageNumber = (pageUsers ?? 1);
            int blogsPageNumber = (pageBlogs ?? 1);

            try
            {
                // 1. Lấy Races chờ duyệt
                var raceResponse = await client.GetAsync("/api/Races/pending");
                List<RaceSummaryDto> pendingRacesList = new List<RaceSummaryDto>();
                if (raceResponse.IsSuccessStatusCode)
                {
                    // SỬA Ở ĐÂY
                    var json = await raceResponse.Content.ReadAsStringAsync();
                    pendingRacesList = JsonConvert.DeserializeObject<List<RaceSummaryDto>>(json) ?? new List<RaceSummaryDto>();
                }
                viewModel.PendingRaces = pendingRacesList.ToPagedList(racesPageNumber, pageSize);

                // 2. Lấy Users
                var userResponse = await client.GetAsync("/api/admin/users");
                List<UserDto> allUsersList = new List<UserDto>();
                if (userResponse.IsSuccessStatusCode)
                {
                    // SỬA Ở ĐÂY
                    var json = await userResponse.Content.ReadAsStringAsync();
                    allUsersList = JsonConvert.DeserializeObject<List<UserDto>>(json) ?? new List<UserDto>();
                }
                viewModel.AllUsers = allUsersList.ToPagedList(usersPageNumber, pageSize);

                // 3. Lấy Blog Posts
                var blogResponse = await client.GetAsync("/api/admin/blogs");
                List<BlogAdminDto> allBlogsList = new List<BlogAdminDto>();
                if (blogResponse.IsSuccessStatusCode)
                {
                    // SỬA Ở ĐÂY
                    var json = await blogResponse.Content.ReadAsStringAsync();
                    allBlogsList = JsonConvert.DeserializeObject<List<BlogAdminDto>>(json) ?? new List<BlogAdminDto>();
                }
                viewModel.AllBlogPosts = allBlogsList.ToPagedList(blogsPageNumber, pageSize);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi tải dữ liệu dashboard: " + ex.Message;
            }

            ViewBag.CurrentTab = tab;
            return View(viewModel);
        }

        // ... (Approve, Cancel Race giữ nguyên vì không đọc response body) ...
        [HttpPost]
        public async Task<IActionResult> Approve(int raceId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            await client.PatchAsync($"/api/Races/{raceId}/approve", null);
            return RedirectToAction("Index", new { tab = "races" });
        }
        [HttpPost]
        public async Task<IActionResult> Cancel(int raceId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            await client.PatchAsync($"/api/Races/{raceId}/cancel", null);
            return RedirectToAction("Index", new { tab = "races" });
        }

        // ===================================
        // QUẢN LÝ NGƯỜI DÙNG (USERS) - Sửa cách đọc response
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
                    // SỬA Ở ĐÂY (Nếu bạn muốn đọc message từ API)
                    var jsonResult = await response.Content.ReadAsStringAsync();
                    var resultObj = JsonConvert.DeserializeObject<dynamic>(jsonResult);
                    TempData["SuccessMessage"] = resultObj?.message ?? "Thay đổi trạng thái tài khoản thành công.";
                }
                catch { TempData["SuccessMessage"] = "Thay đổi trạng thái tài khoản thành công."; }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(); // Đọc lỗi dạng string
                TempData["ErrorMessage"] = "Lỗi: " + error;
            }

            return RedirectToAction("Index", new { tab = "users" });
        }

        // GET: /Admin/UserDetails/5 - Sửa cách đọc response
        public async Task<IActionResult> UserDetails(int userId)
        {
            if (userId <= 0) return BadRequest("User ID không hợp lệ.");
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            var viewModel = new AdminUserDetailViewModel();

            try
            {
                // 1. Lấy thông tin user
                var userResponse = await client.GetAsync($"/api/admin/users/{userId}");
                if (userResponse.IsSuccessStatusCode)
                {
                    // SỬA Ở ĐÂY
                    var json = await userResponse.Content.ReadAsStringAsync();
                    viewModel.User = JsonConvert.DeserializeObject<UserDto>(json);
                    if (viewModel.User == null) throw new Exception("Không thể đọc dữ liệu người dùng.");
                }
                else if (userResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy người dùng với ID={userId}.";
                    return RedirectToAction("Index", new { tab = "users" });
                }
                else
                {
                    var error = await userResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi API khi lấy user: {error}");
                }

                // 2. Lấy tất cả role
                var rolesResponse = await client.GetAsync("/api/admin/roles");
                if (rolesResponse.IsSuccessStatusCode)
                {
                    // SỬA Ở ĐÂY
                    var json = await rolesResponse.Content.ReadAsStringAsync();
                    viewModel.AllRoles = JsonConvert.DeserializeObject<List<RoleDto>>(json);
                    if (viewModel.AllRoles == null) throw new Exception("Không thể đọc danh sách vai trò.");
                }
                else
                {
                    var error = await rolesResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi API khi lấy roles: {error}");
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi tải chi tiết người dùng: " + ex.Message;
                viewModel.User ??= new UserDto { FullName = "Không tải được" };
                viewModel.AllRoles ??= new List<RoleDto>();
            }
            return View(viewModel);
        }

        // POST: /Admin/UpdateUserRoles - Sửa cách đọc response lỗi
        [HttpPost]
        public async Task<IActionResult> UpdateUserRoles(int userId, List<string> selectedRoles)
        {
            if (userId <= 0) return BadRequest("User ID không hợp lệ.");
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var dto = new UpdateUserRolesDto { RoleNames = selectedRoles ?? new List<string>() };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/api/admin/users/{userId}/roles", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Cập nhật vai trò thành công!";
            }
            else
            {
                // SỬA Ở ĐÂY
                string errorMsg = "Cập nhật thất bại.";
                try
                {
                    // Đọc lỗi dạng string trước
                    var errorJson = await response.Content.ReadAsStringAsync();
                    // Thử deserialize thành object để xem có message không
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(errorJson);
                    errorMsg += $" Lý do: {errorObj?.message ?? errorJson}"; // Ưu tiên message, nếu không có thì hiển thị cả JSON lỗi
                }
                catch { /* Bỏ qua nếu không đọc/parse được */ }
                TempData["ErrorMessage"] = errorMsg;
            }
            return RedirectToAction("UserDetails", new { userId = userId });
        }


        // ... (ToggleBlogPostPublish, DeleteBlogPost giữ nguyên vì không đọc response body) ...
        [HttpPost]
        public async Task<IActionResult> ToggleBlogPostPublish(int blogId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            await client.PatchAsync($"/api/admin/blogs/{blogId}/toggle-publish", null);
            return RedirectToAction("Index", new { tab = "blogs" });
        }
        [HttpPost]
        public async Task<IActionResult> DeleteBlogPost(int blogId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");
            await client.DeleteAsync($"/api/admin/blogs/{blogId}");
            return RedirectToAction("Index", new { tab = "blogs" });
        }

        // ... (Hàm CreateAuthenticatedHttpClient giữ nguyên) ...
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