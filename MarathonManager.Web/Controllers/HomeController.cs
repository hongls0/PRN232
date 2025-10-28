using MarathonManager.Web.DTOs;
using MarathonManager.Web.Models;
using MarathonManager.Web.ViewModels; // Quan trọng: Thêm ViewModel
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http; // Thêm IHttpClientFactory
using System.Threading.Tasks; // Thêm Task
using System.Collections.Generic; // Thêm List

namespace MarathonManager.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // Tiêm IHttpClientFactory vào
        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Hàm Index chính, gọi cả 2 API
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient("MarathonApi");

            // 1. Tạo "cái hộp" ViewModel
            var viewModel = new HomePageViewModel();

            try
            {
                // 2. Gọi API Lấy Danh sách Giải chạy (Races)
                var racesResponse = await client.GetAsync("/api/Races");
                if (racesResponse.IsSuccessStatusCode)
                {
                    var jsonString = await racesResponse.Content.ReadAsStringAsync();
                    viewModel.FeaturedRaces = JsonConvert.DeserializeObject<List<RaceSummaryDto>>(jsonString);
                }

                // 3. Gọi API Lấy Danh sách Blog
                var blogResponse = await client.GetAsync("/api/BlogPosts");
                if (blogResponse.IsSuccessStatusCode)
                {
                    var jsonString = await blogResponse.Content.ReadAsStringAsync();
                    viewModel.RecentBlogPosts = JsonConvert.DeserializeObject<List<BlogSummaryDto>>(jsonString);
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi (ví dụ API chưa chạy)
                // Trong một dự án thực tế, bạn nên log lỗi này lại
                // Tạm thời, chúng ta vẫn trả về View rỗng
            }

            // 4. Gửi 1 ViewModel duy nhất (chứa cả 2 list) ra View
            return View(viewModel);
        }

        // Hàm Privacy mặc định
        public IActionResult Privacy()
        {
            return View();
        }

        // Hàm Error mặc định
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}