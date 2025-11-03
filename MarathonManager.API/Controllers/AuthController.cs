using MarathonManager.API.Models;
using MarathonManager.API.DTOs.Auth; // Giả sử bạn tạo DTOs trong thư mục này
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly IConfiguration _configuration;

        // Tiêm (Inject) các dịch vụ cần thiết
        public AuthController(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // ==========================================================
        // POST: api/auth/login
        // ==========================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // 1. Kiểm tra xem người dùng có tồn tại không
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            // 2. Kiểm tra người dùng và mật khẩu
            if (user == null)
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
            }

            // 3. Kiểm tra tài khoản có bị khóa không (logic nghiệp vụ của bạn)
            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin." });
            }

            // 4. Lấy các vai trò (roles) của người dùng
            var userRoles = await _userManager.GetRolesAsync(user);

            // 5. Tạo Token
            var tokenString = GenerateJwtToken(user, userRoles);

            // 6. Trả về Token cho client
            return Ok(new
            {
                token = tokenString,
                user = new
                {
                    email = user.Email,
                    fullName = user.FullName,
                    roles = userRoles
                }
            });
        }

        // ==========================================================
        // POST: api/auth/register
        // ==========================================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // 1. Kiểm tra xem email đã được đăng ký chưa
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                return BadRequest(new { message = "Email đã tồn tại." });
            }

            // 2. Map DTO sang Model User
            User newUser = new User()
            {
                Email = registerDto.Email,
                UserName = registerDto.Email, // Identity yêu cầu UserName
                FullName = registerDto.FullName,
                IsActive = true, // Mặc định là active
                CreatedAt = DateTime.UtcNow
            };

            // 3. Tạo người dùng mới (với mật khẩu đã hash)
            var result = await _userManager.CreateAsync(newUser, registerDto.Password);
            if (!result.Succeeded)
            {
                // Trả về lỗi nếu có
                return BadRequest(new { message = "Tạo tài khoản thất bại.", errors = result.Errors.Select(e => e.Description) });
            }

            // 4. Gán vai trò "Runner" mặc định cho người dùng mới
            // (Chạy hàm SeedRoles trước để đảm bảo Role "Runner" tồn tại)
            await _userManager.AddToRoleAsync(newUser, "Runner");

            return Ok(new { message = "Đăng ký tài khoản thành công." });
        }

        // ==========================================================
        // HÀM PHỤ TRỢ TẠO TOKEN (Yêu cầu 1.10)
        // ==========================================================
        private string GenerateJwtToken(User user, IList<string> roles)
        {
            // 1. Tạo danh sách các "Claims" (thông tin)
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Lấy User ID
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // ID của riêng token này
            };

            // Thêm các Role (vai trò) vào claims
            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // 2. Lấy Khóa bí mật (Secret Key) và thông tin Issuer/Audience từ appsettings.json
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            // 3. Tạo Token
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                expires: DateTime.Now.AddHours(24), // Thời hạn token (ví dụ: 24 giờ)
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            // 4. Ghi token ra dạng chuỗi
            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // ==========================================================
        // [QUAN TRỌNG] CHẠY HÀM NÀY MỘT LẦN ĐỂ TẠO CÁC ROLE
        // POST: api/auth/seed-roles
        // ==========================================================
        [HttpPost]
        [Route("seed-roles")]
        public async Task<IActionResult> SeedRoles()
        {
            bool adminRoleExists = await _roleManager.RoleExistsAsync("Admin");
            bool organizerRoleExists = await _roleManager.RoleExistsAsync("Organizer");
            bool runnerRoleExists = await _roleManager.RoleExistsAsync("Runner");

            if (!adminRoleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Admin"));
            }
            if (!organizerRoleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Organizer"));
            }
            if (!runnerRoleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Runner"));
            }

            // (Tùy chọn) Tạo 1 tài khoản Admin đầu tiên
            var adminUser = await _userManager.FindByEmailAsync("admin@marathon.com");
            if (adminUser == null)
            {
                User admin = new User
                {
                    Email = "admin@marathon.com",
                    UserName = "admin@marathon.com",
                    FullName = "Quản Trị Viên",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await _userManager.CreateAsync(admin, "Admin@123"); // Đặt mật khẩu admin
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            return Ok(new { message = "Tạo Roles (và tài khoản Admin) thành công." });
        }
    }
}