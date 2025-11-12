using MarathonManager.API.DTOs.Race;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MarathonManager.API.DTOs.RaceDistances; // Dùng cho cự ly
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RacesController : ControllerBase
    {
        private readonly MarathonManagerContext _context;
        private readonly IWebHostEnvironment _environment;

        public RacesController(MarathonManagerContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ==========================================================
        // PUBLIC ENDPOINTS
        // ==========================================================

        // GET: api/Races
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetRaces()
        {
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
                    Status = r.Status,
                    OrganizerName = r.Organizer.FullName
                })
                .ToListAsync();
            return Ok(races);
        }

        // GET: api/Races/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<RaceDetailDto>> GetRace(int id)
        {
            var race = await _context.Races
               .Include(r => r.Organizer)
               .Include(r => r.RaceDistances)
               .Where(r => r.Id == id && r.Status == "Approved")
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

            if (race == null) return NotFound(new { message = "Không tìm thấy giải chạy hoặc giải chưa được duyệt." });
            return Ok(race);
        }

        // ==========================================================
        // ORGANIZER ENDPOINTS
        // ==========================================================

        // GET: api/Races/my-races
        [HttpGet("my-races")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetMyRaces()
        {
            var organizerId = GetCurrentUserId();
            var races = await _context.Races
                .Where(r => r.OrganizerId == organizerId)
                .OrderByDescending(r => r.RaceDate)
                .Select(r => new RaceSummaryDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Location = r.Location,
                    RaceDate = r.RaceDate,
                    ImageUrl = r.ImageUrl,
                    Status = r.Status,
                    OrganizerName = r.Organizer.FullName
                })
                .ToListAsync();
            return Ok(races);
        }

        // POST: api/Races
        [HttpPost]
        [Authorize(Roles = "Organizer")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<RaceDetailDto>> PostRace([FromForm] RaceCreateDto raceDto, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var organizerId = GetCurrentUserId();
            string? imageUrlPath = null;

            // 1. Xử lý ảnh (Nếu có)
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif).");
                    return BadRequest(ModelState);
                }
                if (imageFile.Length > 5 * 1024 * 1024) // 5 MB
                {
                    ModelState.AddModelError("imageFile", "Kích thước file ảnh không được vượt quá 5MB.");
                    return BadRequest(ModelState);
                }

                try
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "races");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + extension;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    imageUrlPath = $"/images/races/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
                    ModelState.AddModelError("imageFile", $"Lỗi server khi lưu file ảnh.");
                    return StatusCode(StatusCodes.Status500InternalServerError, ModelState);
                }
            }

            // 2. Tạo Entity Race
            var newRace = new Race
            {
                Name = raceDto.Name,
                Description = raceDto.Description,
                Location = raceDto.Location,
                RaceDate = raceDto.RaceDate,
                ImageUrl = imageUrlPath,
                OrganizerId = organizerId,
                Status = "Pending"
            };
            _context.Races.Add(newRace);
            await _context.SaveChangesAsync();

            // 3. Tạo cự ly từ chuỗi CSV
            List<RaceDistanceDto> createdDistancesDto = new List<RaceDistanceDto>();
            if (!string.IsNullOrWhiteSpace(raceDto.DistancesCsv))
            {
                try
                {
                    var distances = raceDto.DistancesCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var newDistancesList = new List<RaceDistance>();
                    var regex = new Regex(@"[0-9]+(\.[0-9]+)?");

                    foreach (var distString in distances)
                    {
                        var match = regex.Match(distString);
                        if (match.Success && decimal.TryParse(match.Value, out decimal km) && km > 0)
                        {
                            newDistancesList.Add(new RaceDistance
                            {
                                RaceId = newRace.Id,
                                Name = $"{km}km",
                                DistanceInKm = km,
                                RegistrationFee = 0,
                                MaxParticipants = 100,
                                StartTime = newRace.RaceDate.Date.AddHours(6)
                            });
                        }
                    }

                    if (newDistancesList.Any())
                    {
                        _context.RaceDistances.AddRange(newDistancesList);
                        await _context.SaveChangesAsync();
                        createdDistancesDto = newDistancesList.Select(d => new RaceDistanceDto
                        {
                            Id = d.Id,
                            Name = d.Name,
                            DistanceInKm = d.DistanceInKm,
                            RegistrationFee = d.RegistrationFee,
                            MaxParticipants = d.MaxParticipants,
                            StartTime = d.StartTime
                        }).ToList();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing/saving CSV distances: {ex.Message}");
                }
            }

            // 4. Chuẩn bị DTO trả về
            var organizer = await _context.Users.FindAsync(organizerId);
            var resultDto = new RaceDetailDto
            {
                Id = newRace.Id,
                Name = newRace.Name,
                Description = newRace.Description,
                Location = newRace.Location,
                RaceDate = newRace.RaceDate,
                ImageUrl = newRace.ImageUrl,
                Status = newRace.Status,
                OrganizerName = organizer?.FullName ?? "N/A",
                Distances = createdDistancesDto
            };
            return CreatedAtAction(nameof(GetRace), new { id = newRace.Id }, resultDto);
        }

        // PUT: api/Races/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> PutRace(int id, [FromBody] RaceUpdateDto raceDto)
        {
            if (id != raceDto.Id) return BadRequest(new { message = "ID không khớp." });
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound();
            var organizerId = GetCurrentUserId();
            if (race.OrganizerId != organizerId) return Forbid();
            if (race.Status == "Completed") return BadRequest(new { message = "Không thể sửa giải đã hoàn thành." });
            race.Name = raceDto.Name;
            race.Description = raceDto.Description;
            race.Location = raceDto.Location;
            race.RaceDate = raceDto.RaceDate;
            race.Status = "Pending";
            _context.Entry(race).State = EntityState.Modified;
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { if (!RaceExists(id)) return NotFound(); else throw; }
            return NoContent();
        }

        // --- Distance Management Endpoints ---

        // GET: api/Races/{raceId}/distances
        [HttpGet("{raceId}/distances")]
        [Authorize(Roles = "Organizer, Admin")]
        public async Task<ActionResult<IEnumerable<RaceDistanceDto>>> GetDistancesForRace(int raceId)
        {
            var raceExists = await _context.Races.AnyAsync(r => r.Id == raceId);
            if (!raceExists) return NotFound("Không tìm thấy giải chạy.");
            if (User.IsInRole("Organizer"))
            {
                var organizerId = GetCurrentUserId();
                var isOwner = await _context.Races.AnyAsync(r => r.Id == raceId && r.OrganizerId == organizerId);
                if (!isOwner) return Forbid("Bạn không có quyền xem cự ly của giải chạy này.");
            }
            var distances = await _context.RaceDistances
                .Where(d => d.RaceId == raceId)
                .Select(d => new RaceDistanceDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    DistanceInKm = d.DistanceInKm,
                    RegistrationFee = d.RegistrationFee,
                    MaxParticipants = d.MaxParticipants,
                    StartTime = d.StartTime
                })
                .ToListAsync();
            return Ok(distances);
        }

        // GET: api/Races/distance/15 (Lấy 1 cự ly)
        [HttpGet("distance/{distanceId}")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<RaceDistanceDto>> GetDistanceDetails(int distanceId)
        {
            var organizerId = GetCurrentUserId();
            var distance = await _context.RaceDistances
                .Include(d => d.Race)
                .Where(d => d.Id == distanceId)
                .Select(d => new
                {
                    DistanceData = new RaceDistanceDto
                    {
                        Id = d.Id,
                        Name = d.Name,
                        DistanceInKm = d.DistanceInKm,
                        RegistrationFee = d.RegistrationFee,
                        MaxParticipants = d.MaxParticipants,
                        StartTime = d.StartTime
                    },
                    RaceOwnerId = d.Race.OrganizerId
                })
                .FirstOrDefaultAsync();

            if (distance == null) return NotFound(new { message = "Không tìm thấy cự ly." });
            if (distance.RaceOwnerId != organizerId) return Forbid();
            return Ok(distance.DistanceData);
        }

        // POST: api/Races/{raceId}/distances
        [HttpPost("{raceId}/distances")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<RaceDistanceDto>> AddDistanceToRace(int raceId, MarathonManager.API.DTOs.RaceDistances.RaceDistanceCreateDto createDto)
        {
            var race = await _context.Races.FindAsync(raceId);
            if (race == null) return NotFound("Không tìm thấy giải chạy.");
            var organizerId = GetCurrentUserId();
            if (race.OrganizerId != organizerId) return Forbid("Bạn không có quyền thêm cự ly cho giải chạy này.");
            if (race.Status != "Pending" && race.Status != "Approved") return BadRequest("Không thể thêm cự ly.");

            var newDistance = new RaceDistance
            {
                RaceId = raceId,
                Name = createDto.Name,
                DistanceInKm = createDto.DistanceInKm,
                RegistrationFee = createDto.RegistrationFee,
                MaxParticipants = createDto.MaxParticipants,
                StartTime = createDto.StartTime
            };
            _context.RaceDistances.Add(newDistance);
            await _context.SaveChangesAsync();

            var resultDto = new RaceDistanceDto
            {
                Id = newDistance.Id,
                Name = newDistance.Name,
                DistanceInKm = newDistance.DistanceInKm,
                RegistrationFee = newDistance.RegistrationFee,
                MaxParticipants = newDistance.MaxParticipants,
                StartTime = newDistance.StartTime
            };
            return CreatedAtAction(nameof(GetDistancesForRace), new { raceId = raceId }, resultDto);
        }

        // PUT: api/Races/{raceId}/distances/{distanceId}
        [HttpPut("{raceId}/distances/{distanceId}")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> UpdateDistance(int raceId, int distanceId, RaceDistanceUpdateDto updateDto)
        {
            var distance = await _context.RaceDistances.Include(d => d.Race).FirstOrDefaultAsync(d => d.Id == distanceId && d.RaceId == raceId);
            if (distance == null) return NotFound("Không tìm thấy cự ly.");
            var organizerId = GetCurrentUserId();
            if (distance.Race.OrganizerId != organizerId) return Forbid("Không có quyền sửa.");
            if (distance.Race.Status != "Pending" && distance.Race.Status != "Approved") return BadRequest("Không thể sửa cự ly.");

            distance.Name = updateDto.Name;
            distance.DistanceInKm = updateDto.DistanceInKm;
            distance.RegistrationFee = updateDto.RegistrationFee;
            distance.MaxParticipants = updateDto.MaxParticipants;
            distance.StartTime = updateDto.StartTime;

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { if (!RaceExists(distanceId)) return NotFound(); else throw; }
            return NoContent();
        }

        // DELETE: api/Races/{raceId}/distances/{distanceId}
        [HttpDelete("{raceId}/distances/{distanceId}")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> DeleteDistance(int raceId, int distanceId)
        {
            var distance = await _context.RaceDistances.Include(d => d.Race).FirstOrDefaultAsync(d => d.Id == distanceId && d.RaceId == raceId);
            if (distance == null) return NotFound("Không tìm thấy cự ly.");
            var organizerId = GetCurrentUserId();
            if (distance.Race.OrganizerId != organizerId) return Forbid("Không có quyền xóa.");
            if (distance.Race.Status != "Pending" && distance.Race.Status != "Approved") return BadRequest("Không thể xóa cự ly.");
            bool hasRegistrations = await _context.Registrations.AnyAsync(reg => reg.RaceDistanceId == distanceId);
            if (hasRegistrations) return BadRequest("Không thể xóa cự ly đã có người đăng ký.");

            _context.RaceDistances.Remove(distance);
            await _context.SaveChangesAsync();
            return NoContent();
        }


        // ==========================================================
        // ADMIN ENDPOINTS (ĐÃ SỬA LỖI)
        // ==========================================================
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetPendingRaces()
        {
            var races = await _context.Races
               .Include(r => r.Organizer) // Thêm Include
               .Where(r => r.Status == "Pending")
               .Select(r => new RaceSummaryDto
               {
                   Id = r.Id,
                   Name = r.Name,
                   Location = r.Location,
                   RaceDate = r.RaceDate,
                   ImageUrl = r.ImageUrl,
                   Status = r.Status,
                   OrganizerName = r.Organizer.FullName // Lấy tên Organizer
               })
               .ToListAsync();
            return Ok(races); // Trả về
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRace(int id)
        {
            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound(); // Trả về

            race.Status = "Approved";
            _context.Entry(race).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Duyệt giải thành công." }); // Trả về
        }

        [HttpPatch("{id}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelRace(int id)
        {
            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound(); // Trả về

            race.Status = "Cancelled";
            _context.Entry(race).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Hủy giải thành công." }); // Trả về
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRace(int id)
        {
            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound(); // Trả về

            // Xóa ảnh liên quan
            if (!string.IsNullOrEmpty(race.ImageUrl))
            {
                try
                {
                    string imagePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", race.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                catch (Exception ex) { Console.WriteLine($"Lỗi xóa ảnh: {ex.Message}"); }
            }

            _context.Races.Remove(race);
            await _context.SaveChangesAsync();
            return NoContent(); // Trả về
        }

        // ==========================================================
        // HELPER METHODS (ĐÃ SỬA LỖI)
        // ==========================================================
        private bool RaceExists(int id)
        {
            return _context.Races.Any(e => e.Id == id); // Trả về
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                throw new SecurityTokenException("Token không hợp lệ hoặc không chứa ID người dùng.");
            }
            return userId;
        }
    }
}