using MarathonManager.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // <-- THÊM: Cần cho IFormFile
using System.Net.Http;          // <-- THÊM: Cần cho MultipartFormDataContent, StringContent, StreamContent

namespace MarathonManager.Web.Controllers
{
    // Khóa toàn bộ, chỉ Organizer mới vào được
    [Authorize(Roles = "Organizer")]
    public class OrganizerController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrganizerController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        // ===================================
        // QUẢN LÝ GIẢI CHẠY (RACES)
        // ===================================

        // GET: /Organizer/Index
        public async Task<IActionResult> Index()
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account"); // Xử lý token null

            List<RaceSummaryDto> myRaces = new List<RaceSummaryDto>();
            try
            {
                var response = await client.GetAsync("/api/Races/my-races");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    myRaces = JsonConvert.DeserializeObject<List<RaceSummaryDto>>(json) ?? new List<RaceSummaryDto>();
                }
                else
                {
                    // Log lỗi chi tiết hơn nếu cần
                    var errorBody = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Không thể tải danh sách giải chạy ({(int)response.StatusCode}): {errorBody}";
                }
            }
            catch (Exception ex)
            {
                 TempData["ErrorMessage"] = "Lỗi kết nối hoặc xử lý: " + ex.Message;
            }
            return View(myRaces);
        }

        // GET: /Organizer/Create
        public IActionResult Create()
        {
            var model = new RaceCreateDto
            {
                RaceDate = DateTime.Now.AddMonths(1) // Gợi ý ngày chạy
            };
            return View(model);
        }

        // ==========================================================
        // SỬA HÀM POST CREATE ĐỂ GỬI FILE ẢNH
        // ==========================================================
        // POST: /Organizer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RaceCreateDto dto, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            // Kiểm tra file ở Web trước khi gửi đi (tùy chọn nhưng nên làm)
            if (imageFile != null)
            {
                if (imageFile.Length > 5 * 1024 * 1024) // 5 MB
                {
                    ModelState.AddModelError("imageFile", "Kích thước file ảnh không được vượt quá 5MB.");
                    return View(dto);
                }
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif).");
                    return View(dto);
                }
            }

            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            // Dùng using cho content, không dùng cho stream của file
            using var content = new MultipartFormDataContent();

            // Thêm các trường dữ liệu DTO
            content.Add(new StringContent(dto.Name ?? ""), nameof(RaceCreateDto.Name));
            content.Add(new StringContent(dto.Description ?? ""), nameof(RaceCreateDto.Description));
            content.Add(new StringContent(dto.Location ?? ""), nameof(RaceCreateDto.Location));
            content.Add(new StringContent(dto.RaceDate.ToString("o")), nameof(RaceCreateDto.RaceDate)); // ISO 8601

            // Thêm file ảnh (Sửa lỗi Stream)
            if (imageFile != null && imageFile.Length > 0)
            {
                // *** KHÔNG DÙNG USING Ở ĐÂY ***
                var stream = imageFile.OpenReadStream();
                var fileContent = new StreamContent(stream);

                fileContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
                content.Add(fileContent, "imageFile", imageFile.FileName);
            }

            HttpResponseMessage response = null;
            try
            {
                // HttpClient sẽ đọc và giải phóng stream khi gửi
                response = await client.PostAsync("/api/Races", content);
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi kết nối đến API: " + ex.Message);
                return View(dto);
            }

            // Xử lý kết quả (Giống code cũ)
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Tạo giải chạy thành công! Giải đang chờ Admin duyệt.";
                return RedirectToAction("Index");
            }
            else // Xử lý lỗi trả về từ API
            {
                string errorMsg = "Tạo giải chạy thất bại.";
                try
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var validationErrors = JsonConvert.DeserializeObject<ValidationProblemDetails>(errorJson);
                    if (validationErrors != null && validationErrors.Errors.Any())
                    {
                        errorMsg = "Vui lòng kiểm tra lại thông tin nhập.";
                        foreach (var error in validationErrors.Errors)
                        {
                            // Gán lỗi vào đúng trường (ví dụ "imageFile")
                            ModelState.AddModelError(error.Key, string.Join("; ", error.Value));
                        }
                    }
                    else
                    {
                        var errorObj = JsonConvert.DeserializeObject<dynamic>(errorJson);
                        errorMsg += $" Lý do: {errorObj?.message ?? errorJson}";
                    }
                }
                catch { /* Bỏ qua nếu không parse được lỗi */ }

                ModelState.AddModelError(string.Empty, errorMsg);
                return View(dto);
            }
        }
        // ==========================================================
        // KẾT THÚC SỬA HÀM POST CREATE
        // ==========================================================


        // ===================================
        // QUẢN LÝ CỰ LY (DISTANCES) - Giữ nguyên các hàm cũ
        // ===================================

        // GET: /Organizer/ManageDistances/5
        public async Task<IActionResult> ManageDistances(int raceId)
        {
            var client = CreateAuthenticatedHttpClient();
             if (client == null) return RedirectToAction("Login", "Account");

            List<RaceDistanceDto> distances = new List<RaceDistanceDto>();
             try
            {
                var response = await client.GetAsync($"/api/Races/{raceId}/distances");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    distances = JsonConvert.DeserializeObject<List<RaceDistanceDto>>(json) ?? new List<RaceDistanceDto>();
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Không thể tải danh sách cự ly ({(int)response.StatusCode}): {errorBody}";
                    return RedirectToAction("Index");
                }
            } catch (Exception ex) {
                 TempData["ErrorMessage"] = "Lỗi kết nối hoặc xử lý: " + ex.Message;
                 return RedirectToAction("Index");
            }

            ViewBag.RaceId = raceId;
            // TODO: Lấy tên giải chạy từ API để hiển thị
            // ViewBag.RaceName = ...
            return View(distances);
        }

        // GET: /Organizer/AddDistance/5
        public IActionResult AddDistance(int raceId)
        {
             if (raceId <= 0) return BadRequest("Race ID không hợp lệ.");
            var model = new RaceDistanceCreateDto
            {
                RaceId = raceId,
                StartTime = DateTime.Now.Date.AddHours(6) // Gợi ý giờ
            };
            ViewBag.RaceId = raceId;
            return View(model);
        }

        // POST: /Organizer/AddDistance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDistance(RaceDistanceCreateDto dto)
        {
            if (dto.RaceId <= 0) ModelState.AddModelError(nameof(dto.RaceId), "Race ID không hợp lệ.");
            if (!ModelState.IsValid)
            {
                ViewBag.RaceId = dto.RaceId;
                return View(dto);
            }

            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var jsonContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
            HttpResponseMessage response = null;
            try
            {
                response = await client.PostAsync($"/api/Races/{dto.RaceId}/distances", jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Thêm cự ly thành công!";
                    return RedirectToAction("ManageDistances", new { raceId = dto.RaceId });
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Thêm cự ly thất bại ({(int)response.StatusCode}): {errorBody}");
                }
            } catch (Exception ex) {
                 ModelState.AddModelError(string.Empty, "Lỗi kết nối hoặc xử lý: " + ex.Message);
            }

            ViewBag.RaceId = dto.RaceId; // Cần truyền lại RaceId khi lỗi
            return View(dto);
        }

        // GET: /Organizer/EditDistance?distanceId=10&raceId=5
        public async Task<IActionResult> EditDistance(int distanceId, int raceId)
        {
            if (distanceId <= 0 || raceId <= 0) return BadRequest("ID không hợp lệ.");
            var client = CreateAuthenticatedHttpClient();
             if (client == null) return RedirectToAction("Login", "Account");

            RaceDistanceDto distanceDto = null;
            try {
                // !! API GET /api/distances/{distanceId} cần được tạo !!
                var response = await client.GetAsync($"/api/distances/{distanceId}");
                if (response.IsSuccessStatusCode)
                {
                     var json = await response.Content.ReadAsStringAsync();
                     distanceDto = JsonConvert.DeserializeObject<RaceDistanceDto>(json);
                     if (distanceDto == null) throw new Exception("Không thể đọc dữ liệu cự ly.");
                }
                 else if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                 {
                      TempData["ErrorMessage"] = "Không tìm thấy cự ly hoặc bạn không có quyền sửa.";
                      return RedirectToAction("ManageDistances", new { raceId = raceId });
                 }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi API ({(int)response.StatusCode}): {errorBody}");
                }
            } catch (Exception ex) {
                 TempData["ErrorMessage"] = "Lỗi khi tải cự ly để sửa: " + ex.Message;
                 return RedirectToAction("ManageDistances", new { raceId = raceId });
            }

            var model = new RaceDistanceUpdateDto
            {
                Id = distanceDto.Id,
                RaceId = raceId,
                Name = distanceDto.Name,
                DistanceInKm = distanceDto.DistanceInKm,
                RegistrationFee = distanceDto.RegistrationFee,
                MaxParticipants = distanceDto.MaxParticipants,
                StartTime = distanceDto.StartTime
            };
            ViewBag.RaceId = raceId;
            return View(model);
        }

        // POST: /Organizer/EditDistance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDistance(RaceDistanceUpdateDto dto)
        {
            if (dto.Id <= 0 || dto.RaceId <= 0) ModelState.AddModelError(string.Empty, "ID không hợp lệ.");
             if (!ModelState.IsValid)
            {
                 ViewBag.RaceId = dto.RaceId;
                return View(dto);
            }

            var client = CreateAuthenticatedHttpClient();
             if (client == null) return RedirectToAction("Login", "Account");

            var jsonContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
            HttpResponseMessage response = null;
             try {
                response = await client.PutAsync($"/api/Races/{dto.RaceId}/distances/{dto.Id}", jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật cự ly thành công!";
                    return RedirectToAction("ManageDistances", new { raceId = dto.RaceId });
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Cập nhật thất bại ({(int)response.StatusCode}): {errorBody}");
                }
            } catch (Exception ex) {
                 ModelState.AddModelError(string.Empty, "Lỗi kết nối hoặc xử lý: " + ex.Message);
            }

            ViewBag.RaceId = dto.RaceId; // Cần truyền lại RaceId khi lỗi
            return View(dto);
        }

        // POST: /Organizer/DeleteDistance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDistance(int distanceId, int raceId)
        {
             if (distanceId <= 0 || raceId <= 0)
             {
                 TempData["ErrorMessage"] = "ID không hợp lệ.";
                 return RedirectToAction("Index");
             }

            var client = CreateAuthenticatedHttpClient();
             if (client == null) return RedirectToAction("Login", "Account");

            HttpResponseMessage response = null;
             try {
                 response = await client.DeleteAsync($"/api/Races/{raceId}/distances/{distanceId}");
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Xóa cự ly thành công!";
                }
                else
                {
                     var errorBody = await response.Content.ReadAsStringAsync();
                     TempData["ErrorMessage"] = $"Xóa thất bại ({(int)response.StatusCode}): {errorBody}";
                }
            } catch (Exception ex) {
                 TempData["ErrorMessage"] = "Lỗi kết nối hoặc xử lý: " + ex.Message;
            }

            return RedirectToAction("ManageDistances", new { raceId = raceId });
        }


        // ===================================
        // HÀM PHỤ TRỢ (Private)
        // ===================================
        private HttpClient CreateAuthenticatedHttpClient()
        {
            var client = _httpClientFactory.CreateClient("MarathonApi"); // Tên client đã đăng ký trong Program.cs
            try {
                 var token = _httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];

                if (string.IsNullOrEmpty(token))
                {
                    // Log hoặc thông báo lỗi nhẹ nhàng hơn là throw exception
                    // Có thể trả về null và kiểm tra ở nơi gọi
                    // TempData["ErrorMessage"] = "Phiên đăng nhập hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.";
                    return null;
                }

                // Xóa header cũ trước khi thêm mới
                client.DefaultRequestHeaders.Authorization = null;
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                return client;
            } catch (Exception ex) {
                 // Lỗi khi truy cập HttpContext hoặc Cookies (ít xảy ra nhưng nên có)
                 Console.WriteLine($"Error creating authenticated HttpClient: {ex.Message}"); // Dùng ILogger nếu có
                 // TempData["ErrorMessage"] = "Lỗi hệ thống khi xác thực. Vui lòng thử lại.";
                 return null;
            }
        }
    }
}