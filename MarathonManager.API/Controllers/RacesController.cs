using MarathonManager.API.DTOs.Race;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization; // Thêm thư viện Auth
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // Thêm thư viện để lấy Id từ Token
using System.Threading.Tasks;

namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 1. Yêu cầu 1.10: Khóa toàn bộ Controller. Chỉ ai có Token mới được vào
    public class RacesController : ControllerBase
    {
        private readonly MarathonManagerContext _context;

        public RacesController(MarathonManagerContext context)
        {
            _context = context;
        }

        // ==========================================================
        // NGHIỆP VỤ CÔNG KHAI (CHO TẤT CẢ MỌI NGƯỜI)
        // ==========================================================

        // GET: api/Races
        // Lấy danh sách các giải ĐÃ ĐƯỢC DUYỆT (cho trang chủ)
        [HttpGet]
        [AllowAnonymous] // Cho phép người dùng xem mà không cần đăng nhập
        public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetRaces()
        {
            // Yêu cầu 1.6: Dùng DTO, không dùng Entity
            // Chỉ lấy các giải đã được duyệt
            var races = await _context.Races
                .Where(r => r.Status == "Approved")
                .OrderByDescending(r => r.RaceDate)
                .Select(r => new RaceSummaryDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Location = r.Location,
                    RaceDate = r.RaceDate,
                    ImageUrl = r.ImageUrl,
                    Status = r.Status
                })
                .ToListAsync();

            return Ok(races);
        }

        // GET: api/Races/5
        // Lấy chi tiết 1 giải ĐÃ ĐƯỢC DUYỆT (cho trang chi tiết)
        [HttpGet("{id}")]
        [AllowAnonymous] // Cho phép người dùng xem mà không cần đăng nhập
        public async Task<ActionResult<RaceDetailDto>> GetRace(int id)
        {
            // Code này của bạn đã dùng DTO, rất tốt!
            var race = await _context.Races
                .Include(r => r.Organizer) // Lấy thông tin người tổ chức
                .Include(r => r.RaceDistances) // Lấy danh sách các cự ly
                .Where(r => r.Id == id && r.Status == "Approved") // Chỉ lấy giải đã duyệt
                .Select(r => new RaceDetailDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Location = r.Location,
                    RaceDate = r.RaceDate,
                    ImageUrl = r.ImageUrl,
                    Status = r.Status,
                    OrganizerName = r.Organizer.FullName,
                    Distances = r.RaceDistances.Select(d => new RaceDistanceDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        DistanceInKm = d.DistanceInKm,
                        RegistrationFee = d.RegistrationFee,
                        MaxParticipants = d.MaxParticipants,
                        StartTime = d.StartTime
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (race == null)
            {
                // Thay vì "Approved", có thể giải này "Pending" hoặc "Cancelled"
                return NotFound(new { message = "Không tìm thấy giải chạy hoặc giải chưa được duyệt." });
            }

            return Ok(race);
        }

        // ==========================================================
        // NGHIỆP VỤ CHO NGƯỜI TỔ CHỨC (ORGANIZER)
        // ==========================================================

        // GET: api/Races/my-races
        // Lấy danh sách các giải do TÔI (Organizer) tạo ra
        [HttpGet("my-races")]
        [Authorize(Roles = "Organizer")] // Yêu cầu 1.10: Chỉ Organizer
        public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetMyRaces()
        {
            var organizerId = GetCurrentUserId(); // Lấy Id từ Token

            var races = await _context.Races
                .Where(r => r.OrganizerId == organizerId) // Chỉ lấy giải của tôi
                .OrderByDescending(r => r.RaceDate)
                .Select(r => new RaceSummaryDto // Dùng DTO
                {
                    Id = r.Id,
                    Name = r.Name,
                    Location = r.Location,
                    RaceDate = r.RaceDate,
                    ImageUrl = r.ImageUrl,
                    Status = r.Status // Organizer được xem mọi status (Pending, Approved...)
                })
                .ToListAsync();

            return Ok(races);
        }

        // POST: api/Races
        // Organizer tạo một giải chạy mới
        [HttpPost]
        [Authorize(Roles = "Organizer")] // Yêu cầu 1.10: Chỉ Organizer
        public async Task<ActionResult<RaceDetailDto>> PostRace([FromBody] RaceCreateDto raceDto)
        {
            // Yêu cầu 1.7: Kiểm tra Validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var organizerId = GetCurrentUserId(); // Lấy Id từ Token

            // 1. Tạo Entity Race
            var newRace = new Race
            {
                Name = raceDto.Name,
                Description = raceDto.Description,
                Location = raceDto.Location,
                RaceDate = raceDto.RaceDate,
                ImageUrl = raceDto.ImageUrl,
                OrganizerId = organizerId, // Logic nghiệp vụ: Gán người tạo
                Status = "Pending" // Logic nghiệp vụ: Mặc định chờ duyệt
            };

            // 2. Tạo các Entity RaceDistance
            foreach (var distDto in raceDto.Distances)
            {
                newRace.RaceDistances.Add(new RaceDistance
                {
                    Name = distDto.Name,
                    DistanceInKm = distDto.DistanceInKm,
                    RegistrationFee = distDto.RegistrationFee,
                    MaxParticipants = distDto.MaxParticipants,
                    StartTime = distDto.StartTime
                });
            }

            _context.Races.Add(newRace);
            await _context.SaveChangesAsync();

            // Trả về DTO chi tiết (không trả về Entity)
            var resultDto = await GetRace(newRace.Id); // Gọi lại hàm GetRace
            return CreatedAtAction(nameof(GetRace), new { id = newRace.Id }, resultDto.Value);
        }

        // PUT: api/Races/5
        // Organizer cập nhật giải chạy CỦA MÌNH
        [HttpPut("{id}")]
        [Authorize(Roles = "Organizer")] // Chỉ Organizer
        public async Task<IActionResult> PutRace(int id, [FromBody] RaceUpdateDto raceDto)
        {
            if (id != raceDto.Id)
            {
                return BadRequest(new { message = "ID không khớp." });
            }

            // Yêu cầu 1.7: Kiểm tra Validation
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var race = await _context.Races.FindAsync(id);
            if (race == null)
            {
                return NotFound();
            }

            // Yêu cầu 1.10: Kiểm tra xem Organizer có phải chủ của giải này không
            var organizerId = GetCurrentUserId();
            if (race.OrganizerId != organizerId)
            {
                return Forbid(); // Cấm: Bạn không có quyền sửa giải của người khác
            }

            // Chỉ cho phép sửa khi giải chưa diễn ra hoặc đang chờ duyệt
            if (race.Status == "Completed")
            {
                return BadRequest(new { message = "Không thể sửa giải đã hoàn thành." });
            }

            // Map DTO sang Entity
            race.Name = raceDto.Name;
            race.Description = raceDto.Description;
            race.Location = raceDto.Location;
            race.RaceDate = raceDto.RaceDate;
            race.ImageUrl = raceDto.ImageUrl;

            // Nếu sửa, chuyển về chờ duyệt (tùy nghiệp vụ, có thể bỏ)
            // race.Status = "Pending"; 

            _context.Entry(race).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RaceExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // Cập nhật thành công
        }


        // ==========================================================
        // NGHIỆP VỤ CHO QUẢN TRỊ VIÊN (ADMIN)
        // ==========================================================

        // GET: api/Races/pending
        // Admin lấy các giải đang chờ duyệt
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetPendingRaces()
        {
            var races = await _context.Races
                .Where(r => r.Status == "Pending")
                .Select(r => new RaceSummaryDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Location = r.Location,
                    RaceDate = r.RaceDate,
                    Status = r.Status
                })
                .ToListAsync();
            return Ok(races);
        }

        // PATCH: api/Races/5/approve
        // Admin duyệt giải
        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRace(int id)
        {
            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound();

            race.Status = "Approved"; // Nghiệp vụ: Đổi trạng thái
            _context.Entry(race).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Duyệt giải thành công." });
        }

        // PATCH: api/Races/5/cancel
        // Admin hủy giải
        [HttpPatch("{id}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelRace(int id)
        {
            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound();

            race.Status = "Cancelled"; // Nghiệp vụ: Đổi trạng thái
            _context.Entry(race).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Hủy giải thành công." });
        }


        // DELETE: api/Races/5
        // Admin XÓA HẲN giải
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Yêu cầu 1.10: Chỉ Admin được xóa
        public async Task<IActionResult> DeleteRace(int id)
        {
            var race = await _context.Races.FindAsync(id);
            if (race == null)
            {
                return NotFound();
            }

            _context.Races.Remove(race);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ==========================================================
        // HÀM PHỤ TRỢ (Private)
        // ==========================================================

        private bool RaceExists(int id)
        {
            return _context.Races.Any(e => e.Id == id);
        }

        // Hàm phụ trợ để lấy ID của người dùng đang đăng nhập từ Token
        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                throw new SecurityTokenException("Token không hợp lệ hoặc không chứa ID người dùng.");
            }
            return int.Parse(userIdString);
        }
    }
}