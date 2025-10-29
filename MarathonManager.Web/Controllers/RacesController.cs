using MarathonManager.Web.DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MarathonManager.Web.Controllers
{
    public class RacesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RacesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Races/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var client = _httpClientFactory.CreateClient("MarathonaApi");
            var response = await client.GetAsync($"/api/Races/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var raceDetail = JsonConvert.DeserializeObject<RaceDetailDto>(jsonString);
                return View(raceDetail); // Gửi chi tiết ra View
            }

            // Xử lý nếu không tìm thấy
            return NotFound("Không tìm thấy giải chạy.");
        }
    }
}