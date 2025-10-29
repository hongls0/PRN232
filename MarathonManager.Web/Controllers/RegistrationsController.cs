using MarathonManager.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace MarathonManager.Web.Controllers
{
    [Authorize] // Bắt buộc đăng nhập
    public class RegistrationsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RegistrationsController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        // POST: /Registrations/Register
        [HttpPost]
        [Authorize(Roles = "Runner")]
        public async Task<IActionResult> Register(RegistrationCreateDto dto)
        {
            // ... (code gọi API y như cũ)
            var client = CreateAuthenticatedHttpClient();
            var apiDto = new { RaceDistanceId = dto.RaceDistanceId }; // API chỉ cần RaceDistanceId
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(apiDto),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/Registrations", jsonContent);

            // ... (xử lý success/error y như cũ)
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đăng ký thành công!";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                var errorDto = JsonConvert.DeserializeObject<ApiErrorDto>(errorBody);
                TempData["ErrorMessage"] = "Đăng ký thất bại: " + errorDto?.Message;
            }

            // Quay lại trang Chi tiết giải chạy (DÙNG dto.RaceId)
            return RedirectToAction("Detail", "Races", new { id = dto.RaceId });
        }


        // Hàm phụ trợ (Copy từ AdminController)
        private HttpClient CreateAuthenticatedHttpClient()
        {
            var client = _httpClientFactory.CreateClient("MarathonApi");
            var token = _httpContextAccessor.HttpContext.Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Không tìm thấy Token.");
            }
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        // Class tạm để hứng lỗi
        private class ApiErrorDto { public string Message { get; set; } }

        // Hàm phụ trợ (hơi phức tạp, dùng để quay lại đúng trang)
        private async Task<int> GetRaceIdFromDistance(int distanceId)
        {
            // Chúng ta cần gọi 1 API khác để biết RaceId
            // (Bạn cần tạo API GET /api/RaceDistances/{id} trả về RaceDistanceDto)
            // Tạm thời, chúng ta sẽ giả định, hoặc bạn có thể truyền RaceId từ form

            // Cách tốt hơn: Sửa DTO
            // Đổi RegistrationCreateDto thành:
            // public int RaceDistanceId { get; set; }
            // public int RaceId { get; set; } // Thêm trường này

            // Vì hiện tại chưa có, chúng ta tạm trả về 0
            return 0; // Tạm thời
        }
    }
}