using MarathonManager.Web.DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace MarathonManager.Web.Controllers
{
    public class RacesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RacesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ✅ GET: /Races/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ✅ POST: /Races/Create
        [HttpPost]
        public async Task<IActionResult> Create(RaceCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using var client = _httpClientFactory.CreateClient("ApiClient");
            var content = new MultipartFormDataContent();

            content.Add(new StringContent(model.Name), "Name");
            content.Add(new StringContent(model.Description ?? ""), "Description");
            content.Add(new StringContent(model.Location), "Location");
            content.Add(new StringContent(model.RaceDate.ToString("o")), "RaceDate");

            // ✅ Xử lý khoảng cách nhập dạng chuỗi
            var distanceList = model.DistancesInput
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            foreach (var d in distanceList)
            {
                content.Add(new StringContent(d), "Distances");
            }

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var fileContent = new StreamContent(model.ImageFile.OpenReadStream());
                content.Add(fileContent, "ImageFile", model.ImageFile.FileName);
            }

            var response = await client.PostAsync("api/races", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Tạo giải chạy thành công!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Tạo giải chạy thất bại!");
            return View(model);
        }


        // GET: /Races/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var client = _httpClientFactory.CreateClient("MarathonApi");
            var response = await client.GetAsync($"/api/Races/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var raceDetail = JsonConvert.DeserializeObject<RaceDetailDto>(jsonString);
                return View(raceDetail);
            }

            return NotFound("Không tìm thấy giải chạy.");
        }
    }
}
