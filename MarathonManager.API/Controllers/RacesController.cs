using MarathonManager.API.DTOs.Race;
using MarathonManager.API.Models; // Ensure MarathonManagerContext and Race models are here
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MarathonManager.API.DTOs.RaceDistances; // Using for distance DTOs
using Microsoft.AspNetCore.Hosting; // <-- REQUIRED: For IWebHostEnvironment
using System.IO;                  // <-- REQUIRED: For Path, FileStream
using Microsoft.AspNetCore.Http; // <-- REQUIRED: For IFormFile, StatusCodes
using System.Text.RegularExpressions;
using MarathonManager.API.DTOs;
namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Lock down the whole controller
    public class RacesController : ControllerBase
    {
        private readonly MarathonManagerContext _context;
        private readonly IWebHostEnvironment _environment; // To get the wwwroot path

        // Constructor updated to inject IWebHostEnvironment
        public RacesController(MarathonManagerContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment; // Assign injected environment
        }

        // ==========================================================
        // PUBLIC ENDPOINTS
        // ==========================================================

        // GET: api/Races (List approved races)
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
                    ImageUrl = r.ImageUrl, // This will now be the relative path like /images/races/...
                    Status = r.Status
                })
                .ToListAsync();
            return Ok(races);
        }

        // GET: api/Races/5 (Get details of one approved race)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<RaceDetailDto>> GetRace(int id)
        {
            var race = await _context.Races
               .Include(r => r.Organizer) // Include Organizer to get OrganizerName
               .Include(r => r.RaceDistances) // Include Distances
               .Where(r => r.Id == id && r.Status == "Approved")
               .Select(r => new RaceDetailDto
               {
                   Id = r.Id,
                   Name = r.Name,
                   Description = r.Description,
                   Location = r.Location,
                   RaceDate = r.RaceDate,
                   ImageUrl = r.ImageUrl, // Relative path
                   Status = r.Status,
                   OrganizerName = r.Organizer.FullName, // Get name from included Organizer
                   Distances = r.RaceDistances.Select(d => new RaceDistanceDto // Map included distances
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
        // GET: api/Races/my-races
        [HttpGet("my-races")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetMyRaces()
        {
            try
            {
                var organizerId = GetCurrentUserId();

                if (organizerId == null)
                {
                    return Unauthorized(new { message = "Không xác định được người dùng hiện tại." });
                }

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
                        Status = r.Status
                    })
                    .ToListAsync();

                if (races == null || races.Count == 0)
                {
                    return NotFound(new { message = "Không tìm thấy giải chạy nào do bạn tổ chức." });
                }

                return Ok(races);
            }
            catch (Exception ex)
            {
                // Ghi log để dễ debug (nếu có logging)
                Console.WriteLine($"Lỗi trong GetMyRaces: {ex.Message}");

                // Trả thông tin lỗi thân thiện hơn
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi trong quá trình lấy danh sách giải chạy.",
                    detail = ex.Message // Có thể ẩn khi deploy production
                });
            }
        }


        // POST: api/Races
        [HttpPost]
        [Authorize(Roles = "Organizer")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<RaceDetailDto>> PostRace([FromForm] RaceCreateDto raceDto, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var organizerId = GetCurrentUserId();
            string? imageUrlPath = null;

            // ==============================================================
            // PHẦN 1: LƯU ẢNH (Giữ nguyên code cũ)
            // ==============================================================
            if (imageFile != null && imageFile.Length > 0)
            {
                // (Kiểm tra extension, size, v.v...)
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
                // (Lưu file vào wwwroot/images/races...)
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

            // ==============================================================
            // PHẦN 2: LƯU THÔNG TIN GIẢI CHẠY (RACE) (Giữ nguyên)
            // ==============================================================
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
            await _context.SaveChangesAsync(); // <-- LƯU GIẢI CHẠY ĐỂ LẤY ID

            // ==============================================================
            // PHẦN 3: (NÂNG CẤP) TẠO CỰ LY TỪ CHUỖI CSV
            // ==============================================================
            List<RaceDistanceDto> createdDistancesDto = new List<RaceDistanceDto>();
            if (!string.IsNullOrWhiteSpace(raceDto.DistancesCsv))
            {
                try
                {
                    // Tách chuỗi bằng dấu phẩy
                    var distances = raceDto.DistancesCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    var newDistancesList = new List<RaceDistance>();

                    // Dùng Regex để lọc ra chỉ số (kể cả số thập phân)
                    // (VD: "21.1km" -> "21.1")
                    var regex = new Regex(@"[0-9]+(\.[0-9]+)?");

                    foreach (var distString in distances)
                    {
                        var match = regex.Match(distString);

                        // Nếu tìm thấy số và parse thành công
                        if (match.Success && decimal.TryParse(match.Value, out decimal km) && km > 0)
                        {
                            var newDistance = new RaceDistance
                            {
                                RaceId = newRace.Id,
                                Name = $"{km}km", // Tên mặc định (VD: "5km", "21.1km")
                                DistanceInKm = km,
                                RegistrationFee = 0, // Phí mặc định
                                MaxParticipants = 100, // Giới hạn mặc định
                                StartTime = newRace.RaceDate.Date.AddHours(6) // Giờ mặc định
                            };
                            newDistancesList.Add(newDistance);
                        }
                    }

                    if (newDistancesList.Any())
                    {
                        _context.RaceDistances.AddRange(newDistancesList);
                        await _context.SaveChangesAsync(); // Lưu các cự ly

                        // Map sang DTO để trả về
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
                    // Log lỗi nhưng không làm crash request
                    System.Diagnostics.Debug.WriteLine($"Error parsing/saving CSV distances: {ex.Message}");
                }
            }

            // ==============================================================
            // PHẦN 4: CHUẨN BỊ DTO TRẢ VỀ (Giữ nguyên)
            // ==============================================================
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
                Distances = createdDistancesDto // <-- Trả về danh sách cự ly (có thể rỗng)
            };

            return CreatedAtAction(nameof(GetRace), new { id = newRace.Id }, resultDto);
        }

        // PUT: api/Races/5 (Update MY race)
        [HttpPut("{id}")]
        [Authorize(Roles = "Organizer")]
        // NOTE: This currently only accepts JSON ([FromBody]).
        // To allow image updates, it needs to be changed similar to PostRace:
        // - Accept [FromForm] RaceUpdateDto and IFormFile? imageFile
        // - Handle saving the new image file
        // - Handle deleting the old image file (if it exists and a new one is uploaded)
        // - Update the ImageUrl property in the database
        public async Task<IActionResult> PutRace(int id, [FromBody] RaceUpdateDto raceDto)
        {
            if (id != raceDto.Id) return BadRequest(new { message = "ID không khớp." });
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound();

            var organizerId = GetCurrentUserId();
            if (race.OrganizerId != organizerId) return Forbid(); // Check ownership
            if (race.Status == "Completed") return BadRequest(new { message = "Không thể sửa giải đã hoàn thành." });

            // Update properties from DTO
            race.Name = raceDto.Name;
            race.Description = raceDto.Description;
            race.Location = raceDto.Location;
            race.RaceDate = raceDto.RaceDate;
            // ImageUrl update logic would go here if handling file uploads
            // race.ImageUrl = raceDto.ImageUrl; // Only use this if not handling file upload and want to keep existing image

            race.Status = "Pending"; // Set back to Pending for re-approval

            _context.Entry(race).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RaceExists(id)) return NotFound();
                else throw;
            }
            return NoContent(); // Return 204 No Content on successful update
        }

        // --- Distance Management Endpoints ---

        // GET: api/Races/{raceId}/distances
        [HttpGet("{raceId}/distances")]
        [Authorize(Roles = "Organizer, Admin")]
        public async Task<ActionResult<IEnumerable<RaceDistanceDto>>> GetDistancesForRace(int raceId)
        {
            var raceExists = await _context.Races.AnyAsync(r => r.Id == raceId);
            if (!raceExists) return NotFound("Không tìm thấy giải chạy.");

            // Check ownership if the user is an Organizer
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

        // POST: api/Races/{raceId}/distances
        [HttpPost("{raceId}/distances")]
        [Authorize(Roles = "Organizer")]
        // Use fully qualified name if ambiguous reference occurs
        public async Task<ActionResult<RaceDistanceDto>> AddDistanceToRace(int raceId, MarathonManager.API.DTOs.RaceDistances.RaceDistanceCreateDto createDto)
        {
            var race = await _context.Races.FindAsync(raceId);
            if (race == null) return NotFound("Không tìm thấy giải chạy.");

            var organizerId = GetCurrentUserId();
            if (race.OrganizerId != organizerId) return Forbid("Bạn không có quyền thêm cự ly cho giải chạy này.");
            // Allow adding distances only if race is Pending or Approved
            if (race.Status != "Pending" && race.Status != "Approved") return BadRequest("Không thể thêm cự ly khi giải chạy đã hoàn thành hoặc bị hủy.");

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

            // Map the newly created entity back to a DTO to return
            var resultDto = new RaceDistanceDto
            {
                Id = newDistance.Id,
                Name = newDistance.Name,
                DistanceInKm = newDistance.DistanceInKm,
                RegistrationFee = newDistance.RegistrationFee,
                MaxParticipants = newDistance.MaxParticipants,
                StartTime = newDistance.StartTime
            };
            // Return 201 Created with the location of the resource (GetDistancesForRace)
            return CreatedAtAction(nameof(GetDistancesForRace), new { raceId = raceId }, resultDto);
        }

        // PUT: api/Races/{raceId}/distances/{distanceId}
        [HttpPut("{raceId}/distances/{distanceId}")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> UpdateDistance(int raceId, int distanceId, RaceDistanceUpdateDto updateDto)
        {
            // Include Race to check ownership and status
            var distance = await _context.RaceDistances
                                         .Include(d => d.Race)
                                         .FirstOrDefaultAsync(d => d.Id == distanceId && d.RaceId == raceId);
            if (distance == null) return NotFound("Không tìm thấy cự ly hoặc cự ly không thuộc giải chạy này.");

            var organizerId = GetCurrentUserId();
            if (distance.Race.OrganizerId != organizerId) return Forbid("Bạn không có quyền sửa cự ly này.");
            if (distance.Race.Status != "Pending" && distance.Race.Status != "Approved") return BadRequest("Không thể sửa cự ly khi giải chạy đã hoàn thành hoặc bị hủy.");

            // Update properties
            distance.Name = updateDto.Name;
            distance.DistanceInKm = updateDto.DistanceInKm;
            distance.RegistrationFee = updateDto.RegistrationFee;
            distance.MaxParticipants = updateDto.MaxParticipants;
            distance.StartTime = updateDto.StartTime;

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { if (!_context.RaceDistances.Any(e => e.Id == distanceId)) return NotFound(); else throw; }
            return NoContent(); // Success
        }

        // DELETE: api/Races/{raceId}/distances/{distanceId}
        [HttpDelete("{raceId}/distances/{distanceId}")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> DeleteDistance(int raceId, int distanceId)
        {
            var distance = await _context.RaceDistances
                                         .Include(d => d.Race)
                                         .FirstOrDefaultAsync(d => d.Id == distanceId && d.RaceId == raceId);
            if (distance == null) return NotFound("Không tìm thấy cự ly hoặc cự ly không thuộc giải chạy này.");

            var organizerId = GetCurrentUserId();
            if (distance.Race.OrganizerId != organizerId) return Forbid("Bạn không có quyền xóa cự ly này.");
            if (distance.Race.Status != "Pending" && distance.Race.Status != "Approved") return BadRequest("Không thể xóa cự ly khi giải chạy đã hoàn thành hoặc bị hủy.");

            // Business Rule: Prevent deletion if runners are registered
            bool hasRegistrations = await _context.Registrations.AnyAsync(reg => reg.RaceDistanceId == distanceId);
            if (hasRegistrations) return BadRequest("Không thể xóa cự ly đã có vận động viên đăng ký.");

            _context.RaceDistances.Remove(distance);
            await _context.SaveChangesAsync();
            return NoContent(); // Success
        }


        // ==========================================================
        // ADMIN ENDPOINTS
        // ==========================================================
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
                   ImageUrl = r.ImageUrl, // Include ImageUrl here too
                   Status = r.Status
               })
               .ToListAsync();
            return Ok(races);
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRace(int id)
        {
            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound();
            race.Status = "Approved";
            _context.Entry(race).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Duyệt giải thành công." });
        }

        [HttpPatch("{id}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelRace(int id)
        {
            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound();
            race.Status = "Cancelled";
            _context.Entry(race).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Hủy giải thành công." });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRace(int id)
        {
            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound();

            // Optional: Delete associated image file before deleting the race record
            if (!string.IsNullOrEmpty(race.ImageUrl))
            {
                try
                {
                    string imagePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", race.ImageUrl.TrimStart('/')); // Get physical path
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting image file {race.ImageUrl}: {ex.Message}"); // Log error, but continue deletion
                }
            }

            _context.Races.Remove(race);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        // ============================================
        // RUNNER ENDPOINTS
        // ============================================

        /// <summary>
        /// GET: api/Races/runner/dashboard
        /// Get complete dashboard data for runner
        /// </summary>
        [HttpGet("runner/dashboard")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<RunnerDashboardDto>>> GetRunnerDashboard()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(new ApiResponse<RunnerDashboardDto>
                {
                    Success = false,
                    Message = "User not authenticated"
                });

            try
            {
                var dashboard = new RunnerDashboardDto();

                // Get statistics
                var allRegistrations = await _context.Registrations
                    .Include(r => r.RaceDistance)
                    .ThenInclude(rd => rd.Race)
                    .Where(r => r.RunnerId == userId)
                    .ToListAsync();

                dashboard.Statistics = new RunnerStatisticsDto
                {
                    TotalRegistrations = allRegistrations.Count,
                    CompletedRaces = await _context.Results
                        .Where(r => r.Registration.RunnerId == userId && r.Status == "Finished")
                        .CountAsync(),
                    UpcomingRaces = allRegistrations.Count(r =>
                        r.PaymentStatus == "Paid" &&
                        r.RaceDistance.Race.RaceDate > DateTime.Now),
                    PendingRegistrations = allRegistrations.Count(r => r.PaymentStatus == "Pending")
                };

                return Ok(new ApiResponse<RunnerDashboardDto>
                {
                    Success = true,
                    Data = dashboard
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<RunnerDashboardDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching dashboard data",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// GET: api/Races/runner/available
        /// List all approved races available for registration
        /// </summary>
        [HttpGet("runner/available")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<AvailableRaceDto>>>> GetAvailableRaces(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 6)
        {
            var userId = GetCurrentUserId();

            try
            {
                // Get user's registered race IDs
                var registeredRaceIds = await _context.Registrations
                    .Where(r => r.RunnerId == userId && r.PaymentStatus != "Cancelled")
                    .Select(r => r.RaceDistance.RaceId)
                    .Distinct()
                    .ToListAsync();

                // Get available races
                var query = _context.Races
                    .Include(r => r.Organizer)
                    .Include(r => r.RaceDistances)
                    .ThenInclude(rd => rd.Registrations)
                    .Where(r => r.Status == "Approved" && r.RaceDate > DateTime.Now)
                    .OrderBy(r => r.RaceDate);

                var totalCount = await query.CountAsync();
                var races = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var availableRaces = races.Select(r => new AvailableRaceDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Location = r.Location,
                    RaceDate = r.RaceDate,
                    ImageUrl = r.ImageUrl,
                    Status = r.Status,
                    OrganizerId = r.OrganizerId,
                    OrganizerName = r.Organizer.FullName,
                    OrganizerEmail = r.Organizer.Email,
                    IsAlreadyRegistered = registeredRaceIds.Contains(r.Id),
                    Distances = r.RaceDistances.Select(rd => new RaceDistanceSummaryDto
                    {
                        Id = rd.Id,
                        Name = rd.Name,
                        DistanceInKm = rd.DistanceInKm,
                        RegistrationFee = rd.RegistrationFee,
                        MaxParticipants = rd.MaxParticipants,
                        StartTime = rd.StartTime,
                        CurrentParticipants = rd.Registrations.Count(reg => reg.PaymentStatus != "Cancelled"),
                        IsFull = rd.Registrations.Count(reg => reg.PaymentStatus != "Cancelled") >= rd.MaxParticipants
                    }).ToList(),
                    TotalParticipants = r.RaceDistances.Sum(rd => rd.Registrations.Count(reg => reg.PaymentStatus != "Cancelled")),
                    AvailableSlots = r.RaceDistances.Sum(rd => rd.MaxParticipants - rd.Registrations.Count(reg => reg.PaymentStatus != "Cancelled"))
                }).ToList();

                var response = new PaginatedResponse<AvailableRaceDto>
                {
                    Items = availableRaces,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(new ApiResponse<PaginatedResponse<AvailableRaceDto>>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PaginatedResponse<AvailableRaceDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching available races",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// GET: api/Races/{id}/details
        /// Get detailed race information for registration
        /// </summary>
        [HttpGet("{id}/details")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<RaceDetailsDto>>> GetRaceDetails(int id)
        {
            try
            {
                var race = await _context.Races
                    .Include(r => r.Organizer)
                    .Include(r => r.RaceDistances)
                    .ThenInclude(rd => rd.Registrations)
                    .Include(r => r.BlogPosts.Where(bp => bp.Status == "Published"))
                    .ThenInclude(bp => bp.Author)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (race == null)
                    return NotFound(new ApiResponse<RaceDetailsDto>
                    {
                        Success = false,
                        Message = "Race not found"
                    });

                var raceDetail = new RaceDetailsDto
                {
                    Id = race.Id,
                    Name = race.Name,
                    Description = race.Description,
                    Location = race.Location,
                    RaceDate = race.RaceDate,
                    ImageUrl = race.ImageUrl,
                    Status = race.Status,
                    CreatedAt = race.CreatedAt,
                    OrganizerId = race.OrganizerId,
                    OrganizerName = race.Organizer.FullName,
                    OrganizerEmail = race.Organizer.Email,
                    OrganizerPhone = race.Organizer.PhoneNumber,
                    Distances = race.RaceDistances.Select(rd => new RaceDistanceDetailDto
                    {
                        Id = rd.Id,
                        RaceId = rd.RaceId,
                        Name = rd.Name,
                        DistanceInKm = rd.DistanceInKm,
                        RegistrationFee = rd.RegistrationFee,
                        MaxParticipants = rd.MaxParticipants,
                        StartTime = rd.StartTime,
                        CurrentParticipants = rd.Registrations.Count(r => r.PaymentStatus != "Cancelled")
                    }).ToList(),
                    BlogPosts = race.BlogPosts.Select(bp => new BlogPostSummaryDto
                    {
                        Id = bp.Id,
                        Title = bp.Title,
                        FeaturedImageUrl = bp.FeaturedImageUrl,
                        Status = bp.Status,
                        AuthorName = bp.Author.FullName,
                        CreatedAt = bp.CreatedAt
                    }).ToList()
                };

                return Ok(new ApiResponse<RaceDetailsDto>
                {
                    Success = true,
                    Data = raceDetail
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<RaceDetailsDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching race details",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// POST: api/Races/runner/register
        /// Register for a race
        /// </summary>
        [HttpPost("runner/register")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<MyRegistrationDto>>> RegisterForRace(
            [FromBody] RegisterForRaceRequest request)
        {
            var userId = GetCurrentUserId();

            try
            {
                // Check if race distance exists and is available
                var raceDistance = await _context.RaceDistances
                    .Include(rd => rd.Race)
                    .Include(rd => rd.Registrations)
                    .FirstOrDefaultAsync(rd => rd.Id == request.RaceDistanceId);

                if (raceDistance == null)
                    return NotFound(new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "Race distance not found"
                    });

                if (raceDistance.Race.Status != "Approved")
                    return BadRequest(new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "This race is not available for registration"
                    });

                if (raceDistance.Race.RaceDate <= DateTime.Now)
                    return BadRequest(new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "Cannot register for a race that has already occurred"
                    });

                // Check if already registered
                var existingRegistration = await _context.Registrations
                    .FirstOrDefaultAsync(r => r.RunnerId == userId &&
                                            r.RaceDistanceId == request.RaceDistanceId &&
                                            r.PaymentStatus != "Cancelled");

                if (existingRegistration != null)
                    return BadRequest(new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "You have already registered for this race distance"
                    });

                // Check if race distance is full
                var currentParticipants = raceDistance.Registrations
                    .Count(r => r.PaymentStatus != "Cancelled");

                if (currentParticipants >= raceDistance.MaxParticipants)
                    return BadRequest(new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "This race distance is full"
                    });

                // Create registration
                var registration = new Registration
                {
                    RunnerId = userId,
                    RaceDistanceId = request.RaceDistanceId,
                    RegistrationDate = DateTime.Now,
                    PaymentStatus = "Pending"
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync();

                // Return created registration
                var createdRegistration = await _context.Registrations
                    .Include(r => r.RaceDistance)
                    .ThenInclude(rd => rd.Race)
                    .FirstOrDefaultAsync(r => r.Id == registration.Id);

                var registrationDto = new MyRegistrationDto
                {
                    Id = createdRegistration!.Id,
                    RegistrationDate = createdRegistration.RegistrationDate,
                    PaymentStatus = createdRegistration.PaymentStatus,
                    BibNumber = createdRegistration.BibNumber,
                    RaceId = createdRegistration.RaceDistance.RaceId,
                    RaceName = createdRegistration.RaceDistance.Race.Name,
                    Location = createdRegistration.RaceDistance.Race.Location,
                    RaceDate = createdRegistration.RaceDistance.Race.RaceDate,
                    RaceImageUrl = createdRegistration.RaceDistance.Race.ImageUrl,
                    RaceDistanceId = createdRegistration.RaceDistanceId,
                    DistanceName = createdRegistration.RaceDistance.Name,
                    DistanceInKm = createdRegistration.RaceDistance.DistanceInKm,
                    RegistrationFee = createdRegistration.RaceDistance.RegistrationFee,
                    StartTime = createdRegistration.RaceDistance.StartTime,
                    CanCancel = createdRegistration.RaceDistance.Race.RaceDate > DateTime.Now,
                    HasResult = false,
                    DisplayStatus = "Pending Payment"
                };

                return Ok(new ApiResponse<MyRegistrationDto>
                {
                    Success = true,
                    Message = "Successfully registered for the race. Please complete payment.",
                    Data = registrationDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<MyRegistrationDto>
                {
                    Success = false,
                    Message = "An error occurred while registering for the race",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// GET: api/Races/runner/registrations
        /// Get all registrations for current runner
        /// </summary>
        [HttpGet("runner/registrations")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<MyRegistrationDto>>>> GetMyRegistrations(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();

            try
            {
                var query = _context.Registrations
                    .Include(r => r.RaceDistance)
                    .ThenInclude(rd => rd.Race)
                    .Include(r => r.Result)
                    .Where(r => r.RunnerId == userId)
                    .OrderByDescending(r => r.RegistrationDate);

                var totalCount = await query.CountAsync();
                var registrations = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var registrationDtos = registrations.Select(r => new MyRegistrationDto
                {
                    Id = r.Id,
                    RegistrationDate = r.RegistrationDate,
                    PaymentStatus = r.PaymentStatus,
                    BibNumber = r.BibNumber,
                    RaceId = r.RaceDistance.RaceId,
                    RaceName = r.RaceDistance.Race.Name,
                    Location = r.RaceDistance.Race.Location,
                    RaceDate = r.RaceDistance.Race.RaceDate,
                    RaceImageUrl = r.RaceDistance.Race.ImageUrl,
                    RaceDistanceId = r.RaceDistanceId,
                    DistanceName = r.RaceDistance.Name,
                    DistanceInKm = r.RaceDistance.DistanceInKm,
                    RegistrationFee = r.RaceDistance.RegistrationFee,
                    StartTime = r.RaceDistance.StartTime,
                    CanCancel = r.RaceDistance.Race.RaceDate > DateTime.Now && r.PaymentStatus != "Cancelled",
                    HasResult = r.Result != null,
                    DisplayStatus = r.PaymentStatus switch
                    {
                        "Pending" => "Pending Payment",
                        "Paid" => r.RaceDistance.Race.RaceDate > DateTime.Now ? "Confirmed" : "Completed",
                        "Cancelled" => "Cancelled",
                        _ => r.PaymentStatus
                    }
                }).ToList();

                var response = new PaginatedResponse<MyRegistrationDto>
                {
                    Items = registrationDtos,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(new ApiResponse<PaginatedResponse<MyRegistrationDto>>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PaginatedResponse<MyRegistrationDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching registrations",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// DELETE: api/Races/runner/registrations/{id}
        /// Cancel a registration
        /// </summary>
        [HttpDelete("runner/registrations/{id}")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<object>>> CancelRegistration(int id)
        {
            var userId = GetCurrentUserId();

            try
            {
                var registration = await _context.Registrations
                    .Include(r => r.RaceDistance)
                    .ThenInclude(rd => rd.Race)
                    .FirstOrDefaultAsync(r => r.Id == id && r.RunnerId == userId);

                if (registration == null)
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Registration not found"
                    });

                if (registration.RaceDistance.Race.RaceDate <= DateTime.Now)
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Cannot cancel registration for a race that has already occurred"
                    });

                if (registration.PaymentStatus == "Cancelled")
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Registration is already cancelled"
                    });

                registration.PaymentStatus = "Cancelled";
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Registration cancelled successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while cancelling registration",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// GET: api/Races/runner/results
        /// Get all results for current runner
        /// </summary>
        [HttpGet("runner/results")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<MyResultDto>>>> GetMyResults(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetCurrentUserId();

            try
            {
                var query = _context.Results
                    .Include(r => r.Registration)
                    .ThenInclude(reg => reg.RaceDistance)
                    .ThenInclude(rd => rd.Race)
                    .Where(r => r.Registration.RunnerId == userId)
                    .OrderByDescending(r => r.Registration.RaceDistance.Race.RaceDate);

                var totalCount = await query.CountAsync();
                var results = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var resultDtos = results.Select(r => new MyResultDto
                {
                    Id = r.Id,
                    RegistrationId = r.RegistrationId,
                    CompletionTime = r.CompletionTime,
                    OverallRank = r.OverallRank,
                    GenderRank = r.GenderRank,
                    AgeCategoryRank = r.AgeCategoryRank,
                    Status = r.Status,
                    RaceId = r.Registration.RaceDistance.RaceId,
                    RaceName = r.Registration.RaceDistance.Race.Name,
                    Location = r.Registration.RaceDistance.Race.Location,
                    RaceDate = r.Registration.RaceDistance.Race.RaceDate,
                    DistanceName = r.Registration.RaceDistance.Name,
                    DistanceInKm = r.Registration.RaceDistance.DistanceInKm,
                    FormattedTime = r.CompletionTime?.ToString(@"hh\:mm\:ss"),
                    AveragePace = r.CompletionTime.HasValue && r.Registration.RaceDistance.DistanceInKm > 0
                        ? $"{(r.CompletionTime.Value.ToTimeSpan().TotalMinutes / (double)r.Registration.RaceDistance.DistanceInKm):F2} min/km"
                        : null
                }).ToList();

                var response = new PaginatedResponse<MyResultDto>
                {
                    Items = resultDtos,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(new ApiResponse<PaginatedResponse<MyResultDto>>
                {
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PaginatedResponse<MyResultDto>>
                {
                    Success = false,
                    Message = "An error occurred while fetching results",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
        // ==========================================================
        // HELPER METHODS
        // ==========================================================
        private bool RaceExists(int id)
        {
            return _context.Races.Any(e => e.Id == id);
        }

        // Get User ID from JWT Token Claim
        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            {
                // This should not happen if [Authorize] is working correctly
                throw new SecurityTokenException("Invalid token or missing user ID claim.");
            }
            return userId;
        }
    }
}