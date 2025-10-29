using MarathonManager.API.DTOs.Registration; // Đảm bảo using đúng
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly MarathonManagerContext _context;

    public RegistrationsController(MarathonManagerContext context)
    {
        _context = context;
    }

    // POST: api/Registrations
    [HttpPost]
    [Authorize(Roles = "Runner")]
    // THAY ĐỔI TÊN DTO Ở ĐÂY
    public async Task<IActionResult> RegisterForRace([FromBody] RunnerRegistrationRequestDto registrationDto)
    {
        var runnerId = GetCurrentUserId();

        // 1. Kiểm tra cự ly
        var raceDistance = await _context.RaceDistances
            .Include(rd => rd.Race)
            .FirstOrDefaultAsync(rd => rd.Id == registrationDto.RaceDistanceId); // Dùng DTO mới

        if (raceDistance == null || raceDistance.Race.Status != "Approved")
        {
            return NotFound(new { message = "Cự ly này không tồn tại hoặc giải chạy chưa được duyệt." });
        }

        // 2. Kiểm tra đã đăng ký chưa
        bool isAlreadyRegistered = await _context.Registrations
            .AnyAsync(r => r.RunnerId == runnerId && r.RaceDistanceId == registrationDto.RaceDistanceId); // Dùng DTO mới

        if (isAlreadyRegistered)
        {
            return BadRequest(new { message = "Bạn đã đăng ký cự ly này rồi." });
        }

        // 3. Kiểm tra số lượng
        int currentRegistrations = await _context.Registrations
            .CountAsync(r => r.RaceDistanceId == registrationDto.RaceDistanceId); // Dùng DTO mới

        if (currentRegistrations >= raceDistance.MaxParticipants)
        {
            return BadRequest(new { message = "Cự ly này đã hết chỗ." });
        }

        // 4. Tạo đăng ký
        var newRegistration = new Registration
        {
            RunnerId = runnerId,
            RaceDistanceId = registrationDto.RaceDistanceId, // Dùng DTO mới
            RegistrationDate = DateTime.UtcNow,
            PaymentStatus = "Pending"
        };

        _context.Registrations.Add(newRegistration);
        await _context.SaveChangesAsync();

        // Có thể trả về thông tin đăng ký vừa tạo nếu cần
        // var resultDto = new RegistrationDto { ... map từ newRegistration ... };
        // return CreatedAtAction(nameof(GetMyRegistrationById), new { id = newRegistration.Id }, resultDto); // Cần tạo hàm GetMyRegistrationById

        return Ok(new { message = "Đăng ký thành công! Vui lòng chờ thanh toán." });
    }

    // ... (Hàm GetCurrentUserId giữ nguyên) ...
    private int GetCurrentUserId()
    {
        // Thêm kiểm tra Parse an toàn hơn
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
        {
            return userId;
        }
        throw new InvalidOperationException("Không thể xác định ID người dùng từ token.");
    }
}