using MarathonManager.API.DTOs.Registration;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Chỉ ai đăng nhập mới được dùng
public class RegistrationsController : ControllerBase
{
    private readonly MarathonManagerContext _context;

    public RegistrationsController(MarathonManagerContext context)
    {
        _context = context;
    }

    // POST: api/Registrations
    // VĐV (Runner) đăng ký 1 cự ly
    [HttpPost]
    [Authorize(Roles = "Runner")] // Chỉ VĐV
    public async Task<IActionResult> RegisterForRace([FromBody] RegistrationCreateDto registrationDto)
    {
        var runnerId = GetCurrentUserId();

        // 1. Kiểm tra cự ly có tồn tại và thuộc giải đã được duyệt không
        var raceDistance = await _context.RaceDistances
            .Include(rd => rd.Race) // Lấy thông tin giải chạy
            .FirstOrDefaultAsync(rd => rd.Id == registrationDto.RaceDistanceId);

        if (raceDistance == null || raceDistance.Race.Status != "Approved")
        {
            return NotFound(new { message = "Cự ly này không tồn tại hoặc giải chạy chưa được duyệt." });
        }

        // 2. Kiểm tra VĐV đã đăng ký cự ly này chưa
        bool isAlreadyRegistered = await _context.Registrations
            .AnyAsync(r => r.RunnerId == runnerId && r.RaceDistanceId == registrationDto.RaceDistanceId);

        if (isAlreadyRegistered)
        {
            return BadRequest(new { message = "Bạn đã đăng ký cự ly này rồi." });
        }

        // 3. Kiểm tra số lượng tối đa
        int currentRegistrations = await _context.Registrations
            .CountAsync(r => r.RaceDistanceId == registrationDto.RaceDistanceId);

        if (currentRegistrations >= raceDistance.MaxParticipants)
        {
            return BadRequest(new { message = "Cự ly này đã hết chỗ." });
        }

        // 4. Tạo đăng ký mới
        var newRegistration = new Registration
        {
            RunnerId = runnerId,
            RaceDistanceId = registrationDto.RaceDistanceId,
            RegistrationDate = DateTime.UtcNow,
            PaymentStatus = "Pending" // Chờ thanh toán
        };

        _context.Registrations.Add(newRegistration);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đăng ký thành công! Vui lòng chờ thanh toán." });
    }

    // (Bạn có thể thêm hàm GET "api/Registrations/my-registrations" 
    //  để VĐV xem các giải mình đã đăng ký)

    // Hàm phụ trợ
    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}