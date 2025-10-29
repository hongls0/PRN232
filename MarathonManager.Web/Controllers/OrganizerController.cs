using MarathonManager.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

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

        // GET: /Organizer/Index
        // Hiển thị các giải chạy "của tôi"
        public async Task<IActionResult> Index()
        {
            var client = CreateAuthenticatedHttpClient();
            var response = await client.GetAsync("/api/Races/my-races");

            List<RaceSummaryDto> myRaces = new List<RaceSummaryDto>();
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                myRaces = JsonConvert.DeserializeObject<List<RaceSummaryDto>>(json);
            }

            return View(myRaces);
        }

        // GET: /Organizer/Create
        // Hiển thị form để tạo giải chạy mới
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Organizer/Create
        // Nhận dữ liệu từ form và gọi API
        [HttpPost]
        public async Task<IActionResult> Create(RaceCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var client = CreateAuthenticatedHttpClient();
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(dto),
                Encoding.UTF8, "application/json");

            // Gọi API POST /api/Races (đã có sẵn [Authorize(Roles="Organizer")])
            var response = await client.PostAsync("/api/Races", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                // Tạo thành công, quay về trang Dashboard của Organizer
                return RedirectToAction("Index");
            }
            else
            {
                // Thêm lỗi nếu API báo thất bại
                ModelState.AddModelError(string.Empty, "Tạo giải chạy thất bại. Vui lòng thử lại.");
                return View(dto);
            }
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
    }
}