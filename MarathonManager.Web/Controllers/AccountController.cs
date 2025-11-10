using MarathonManager.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net.Http; // Cần cho StringContent
using System.Threading.Tasks; // Cần cho Task
using System.Linq; // Cần cho FirstOrDefault

namespace MarathonManager.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        // _environment không cần thiết trong AccountController

        public AccountController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        // ===================================
        // GET: /Account/Login
        // ===================================
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ===================================
        // POST: /Account/Login
        // ===================================
        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm AntiForgeryToken
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("MarathonApi");

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(model),
                Encoding.UTF8, "application/json");

            var apiResponse = await client.PostAsync("/api/auth/login", jsonContent);

            if (apiResponse.IsSuccessStatusCode)
            {
                var jsonResponse = await apiResponse.Content.ReadAsStringAsync();
                var tokenDto = JsonConvert.DeserializeObject<TokenResponseDto>(jsonResponse);

                await SignInUserAsync(tokenDto.Token);

                return LocalRedirect(returnUrl);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }
        }

        // ===================================
        // GET: /Account/Register
        // ===================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ===================================
        // POST: /Account/Register
        // ===================================
        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm AntiForgeryToken
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("MarathonApi");

            var apiDto = new { model.FullName, model.Email, model.Password };

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(apiDto),
                Encoding.UTF8, "application/json");

            var apiResponse = await client.PostAsync("/api/auth/register", jsonContent);

            if (apiResponse.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            else
            {
                // Thêm lỗi chi tiết từ API (nếu có)
                string errorMsg = "Đăng ký thất bại. Email có thể đã tồn tại.";
                try
                {
                    var errorJson = await apiResponse.Content.ReadAsStringAsync();
                    var errorObj = JsonConvert.DeserializeObject<dynamic>(errorJson);
                    // (Giả sử API trả về lỗi validation chi tiết)
                    if (errorObj?.errors != null)
                    {
                        // Ép kiểu errorObj.errors và cả e.description
                        errorMsg = string.Join(" ", ((IEnumerable<dynamic>)errorObj.errors).Select(e => (string)e.description));
                    }
                }
                catch { /* Bỏ qua nếu không parse được lỗi */ }

                ModelState.AddModelError(string.Empty, errorMsg);
                return View(model);
            }
        }

        // ===================================
        // POST: /Account/Logout
        // ===================================
        [HttpPost]
        [ValidateAntiForgeryToken] // Thêm AntiForgeryToken
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _httpContextAccessor.HttpContext.Response.Cookies.Delete("AuthToken");
            return RedirectToAction("Index", "Home");
        }


        // ===================================
        // HÀM PHỤ TRỢ (Private)
        // ===================================
        private async Task SignInUserAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Thêm claim "Name" (loại mặc định) để _Layout có thể hiển thị @User.Identity.Name
            var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name");
            if (nameClaim != null)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, nameClaim.Value));
            }

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            };

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _httpContextAccessor.HttpContext.Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(24)
            });
        }

        // Class tạm thời để hứng token
        private class TokenResponseDto
        {
            [JsonProperty("token")]
            public string Token { get; set; }
        }
    }
}