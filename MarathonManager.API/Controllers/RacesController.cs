using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarathonManager.API.Models;
using MarathonManager.API.DTOs;
using System.Security.Claims;
using MarathonManager.API.DTOs.Race;

namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RacesController : ControllerBase
    {
        private readonly MarathonManagerContext _context;

        public RacesController(MarathonManagerContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
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
        public async Task<ActionResult<ApiResponse<RaceDetailDto>>> GetRaceDetails(int id)
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
                    return NotFound(new ApiResponse<RaceDetailDto>
                    {
                        Success = false,
                        Message = "Race not found"
                    });

                var blogs = await _context.BlogPosts
    .Include(b => b.Author)
    .Where(b => b.Status == "Published" && b.RaceId == race.Id)
    .OrderByDescending(b => b.CreatedAt)
    .Take(6)
    .Select(b => new BlogListItemDto
    {
        Id = b.Id,
        Title = b.Title,
        Excerpt = b.Content.Length > 120 ? b.Content.Substring(0, 120) + "..." : b.Content,
        FeaturedImageUrl = b.FeaturedImageUrl,
        CreatedAt = b.CreatedAt,
        AuthorName = b.Author.FullName,
        LikeCount = b.Likes.Count(),
        CommentCount = b.Comments.Count(),
        IsLikedByCurrentUser = false, // hoặc tính theo user nếu cần
        RaceId = b.RaceId,
        RaceName = b.Race != null ? b.Race.Name : null
    })
    .ToListAsync();


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
                    BlogPosts = blogs
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

                // Lấy registration gần nhất của runner cho cự ly này (bất kể status)
                var existingRegistration = await _context.Registrations
                    .Include(r => r.RaceDistance)
                        .ThenInclude(rd => rd.Race)
                    .Where(r => r.RunnerId == userId && r.RaceDistanceId == request.RaceDistanceId)
                    .OrderByDescending(r => r.RegistrationDate)
                    .FirstOrDefaultAsync();

                // Nếu đã có đăng ký ACTIVE (Pending/Paid/Confirmed/...) thì chặn
                if (existingRegistration != null && existingRegistration.PaymentStatus != "Cancelled")
                {
                    return BadRequest(new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "You have already registered for this race distance"
                    });
                }

                // Kiểm tra đủ chỗ (chỉ đếm non-cancelled)
                var currentParticipants = await _context.Registrations
                    .CountAsync(r => r.RaceDistanceId == request.RaceDistanceId
                                     && r.PaymentStatus != "Cancelled");

                if (currentParticipants >= raceDistance.MaxParticipants)
                {
                    return BadRequest(new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "This race distance is full"
                    });
                }

                // Nếu có registration Cancelled => tái sử dụng (re-activate)
                // Nếu không có => tạo mới
                Registration registration;

                if (existingRegistration != null && existingRegistration.PaymentStatus == "Cancelled")
                {
                    // Re-activate cái đã cancel
                    registration = existingRegistration;
                    registration.PaymentStatus = "Pending";
                    registration.RegistrationDate = DateTime.Now;
                    registration.BibNumber = null; // nếu muốn reset bib khi đăng ký lại
                }
                else
                {
                    // Tạo registration mới
                    registration = new Registration
                    {
                        RunnerId = userId,
                        RaceDistanceId = request.RaceDistanceId,
                        RegistrationDate = DateTime.Now,
                        PaymentStatus = "Pending"
                    };

                    _context.Registrations.Add(registration);
                }

                await _context.SaveChangesAsync();

                // (Nếu dùng existingRegistration.Include ở trên thì registration đã có đầy đủ navigation)
                // Nếu không chắc, có thể load lại:
                registration = await _context.Registrations
                    .Include(r => r.RaceDistance)
                        .ThenInclude(rd => rd.Race)
                    .FirstAsync(r => r.Id == registration.Id);

                // Build DTO
                var registrationDto = new MyRegistrationDto
                {
                    Id = registration.Id,
                    RegistrationDate = registration.RegistrationDate,
                    PaymentStatus = registration.PaymentStatus,
                    BibNumber = registration.BibNumber,
                    RaceId = registration.RaceDistance.RaceId,
                    RaceName = registration.RaceDistance.Race.Name,
                    Location = registration.RaceDistance.Race.Location,
                    RaceDate = registration.RaceDistance.Race.RaceDate,
                    RaceImageUrl = registration.RaceDistance.Race.ImageUrl,
                    RaceDistanceId = registration.RaceDistanceId,
                    DistanceName = registration.RaceDistance.Name,
                    DistanceInKm = registration.RaceDistance.DistanceInKm,
                    RegistrationFee = registration.RaceDistance.RegistrationFee,
                    StartTime = registration.RaceDistance.StartTime,
                    CanCancel = registration.RaceDistance.Race.RaceDate > DateTime.Now,
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
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
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

        /// <summary>
        /// POST: api/Races/runner/registrations/{registrationId}/fake-payment
        /// Đánh dấu registration là "Paid" + cấp BibNumber (dùng để test)
        /// </summary>
        [HttpPost("runner/registrations/{registrationId}/fake-payment")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<MyRegistrationDto>>> FakePayment(int registrationId)
        {
            var userId = GetCurrentUserId();

            try
            {
                var registration = await _context.Registrations
                    .Include(r => r.RaceDistance)
                        .ThenInclude(rd => rd.Race)
                    .FirstOrDefaultAsync(r => r.Id == registrationId && r.RunnerId == userId);

                if (registration == null)
                {
                    return NotFound(new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "Registration not found for current runner"
                    });
                }

                // Nếu đã Paid rồi thì không làm lại nữa
                if (registration.PaymentStatus == "Paid")
                {
                    return BadRequest(new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "Registration is already paid"
                    });
                }

                // (Tuỳ bạn) nếu hiện đang Cancelled thì cho/không cho fake payment
                if (registration.PaymentStatus == "Cancelled")
                {
                    return BadRequest(new ApiResponse<MyRegistrationDto>
                    {
                        Success = false,
                        Message = "Cannot pay for a cancelled registration"
                    });
                }

                // Đánh dấu đã thanh toán
                registration.PaymentStatus = "Paid";

                // Cấp BibNumber nếu chưa có
                if (string.IsNullOrEmpty(registration.BibNumber))
                {
                    var random = new Random();
                    registration.BibNumber = random.Next(10000, 99999).ToString();
                }

                await _context.SaveChangesAsync();

                // Map sang MyRegistrationDto (giống đoạn bạn dùng trong RegisterForRace)
                var dto = new MyRegistrationDto
                {
                    Id = registration.Id,
                    RegistrationDate = registration.RegistrationDate,
                    PaymentStatus = registration.PaymentStatus,
                    BibNumber = registration.BibNumber,
                    RaceId = registration.RaceDistance.RaceId,
                    RaceName = registration.RaceDistance.Race.Name,
                    Location = registration.RaceDistance.Race.Location,
                    RaceDate = registration.RaceDistance.Race.RaceDate,
                    RaceImageUrl = registration.RaceDistance.Race.ImageUrl,
                    RaceDistanceId = registration.RaceDistanceId,
                    DistanceName = registration.RaceDistance.Name,
                    DistanceInKm = registration.RaceDistance.DistanceInKm,
                    RegistrationFee = registration.RaceDistance.RegistrationFee,
                    StartTime = registration.RaceDistance.StartTime,
                    CanCancel = registration.RaceDistance.Race.RaceDate > DateTime.Now,
                    HasResult = registration.Result != null,
                    DisplayStatus = registration.PaymentStatus == "Paid"
                        ? "Paid"
                        : registration.PaymentStatus
                };

                return Ok(new ApiResponse<MyRegistrationDto>
                {
                    Success = true,
                    Message = "Fake payment successful",
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<MyRegistrationDto>
                {
                    Success = false,
                    Message = "An error occurred while processing fake payment",
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
                });
            }
        }

        /// <summary>
        /// GET: api/Races/runner/profile
        /// Get runner profile with statistics
        /// </summary>
        [HttpGet("runner/profile")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<RunnerProfileDto>>> GetRunnerProfile()
        {
            var userId = GetCurrentUserId();

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new ApiResponse<RunnerProfileDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });

                // Calculate age
                int? age = null;
                if (user.DateOfBirth.HasValue)
                {
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    age = today.Year - user.DateOfBirth.Value.Year;
                    if (user.DateOfBirth.Value > today.AddYears(-age.Value))
                        age--;
                }

                // Get all registrations
                var registrations = await _context.Registrations
                    .Include(r => r.RaceDistance)
                    .ThenInclude(rd => rd.Race)
                    .Where(r => r.RunnerId == userId)
                    .ToListAsync();

                // Get all results
                var results = await _context.Results
                    .Include(r => r.Registration)
                    .ThenInclude(reg => reg.RaceDistance)
                    .ThenInclude(rd => rd.Race)
                    .Where(r => r.Registration.RunnerId == userId)
                    .ToListAsync();

                // Calculate statistics
                var statistics = new RunnerProfileStatisticsDto
                {
                    TotalRegistrations = registrations.Count,
                    ActiveRegistrations = registrations.Count(r => r.PaymentStatus == "Paid" && r.RaceDistance.Race.RaceDate > DateTime.Now),
                    CompletedRaces = results.Count(r => r.Status == "Finished"),
                    CancelledRegistrations = registrations.Count(r => r.PaymentStatus == "Cancelled"),
                    TotalRacesFinished = results.Count(r => r.Status == "Finished"),
                    TotalDistanceRun = results
                        .Where(r => r.Status == "Finished")
                        .Sum(r => r.Registration.RaceDistance.DistanceInKm),
                    Top3Finishes = results.Count(r => r.OverallRank.HasValue && r.OverallRank <= 3),
                    Top10Finishes = results.Count(r => r.OverallRank.HasValue && r.OverallRank <= 10),
                    DaysSinceJoined = (DateTime.Now - user.CreatedAt).Days,
                    YearsActive = (DateTime.Now - user.CreatedAt).Days / 365
                };

                // Calculate best times by distance category
                var finishedResults = results.Where(r => r.Status == "Finished" && r.CompletionTime.HasValue).ToList();

                statistics.Best5K = finishedResults
                    .Where(r => r.Registration.RaceDistance.DistanceInKm >= 4.5m && r.Registration.RaceDistance.DistanceInKm <= 5.5m)
                    .OrderBy(r => r.CompletionTime)
                    .FirstOrDefault()?.CompletionTime;

                statistics.Best10K = finishedResults
                    .Where(r => r.Registration.RaceDistance.DistanceInKm >= 9.5m && r.Registration.RaceDistance.DistanceInKm <= 10.5m)
                    .OrderBy(r => r.CompletionTime)
                    .FirstOrDefault()?.CompletionTime;

                statistics.BestHalfMarathon = finishedResults
                    .Where(r => r.Registration.RaceDistance.DistanceInKm >= 20m && r.Registration.RaceDistance.DistanceInKm <= 22m)
                    .OrderBy(r => r.CompletionTime)
                    .FirstOrDefault()?.CompletionTime;

                statistics.BestMarathon = finishedResults
                    .Where(r => r.Registration.RaceDistance.DistanceInKm >= 41m && r.Registration.RaceDistance.DistanceInKm <= 43m)
                    .OrderBy(r => r.CompletionTime)
                    .FirstOrDefault()?.CompletionTime;

                // Get recent activities (last 10)
                var recentActivities = new List<RecentActivityDto>();

                // Recent registrations
                foreach (var reg in registrations.OrderByDescending(r => r.RegistrationDate).Take(5))
                {
                    recentActivities.Add(new RecentActivityDto
                    {
                        ActivityType = "Registration",
                        Description = $"Registered for {reg.RaceDistance.Race.Name} ({reg.RaceDistance.Name})",
                        ActivityDate = reg.RegistrationDate,
                        Icon = "fa-user-plus",
                        BadgeClass = "bg-primary"
                    });
                }

                // Recent results
                foreach (var result in results.OrderByDescending(r => r.Registration.RaceDistance.Race.RaceDate).Take(5))
                {
                    recentActivities.Add(new RecentActivityDto
                    {
                        ActivityType = "Result",
                        Description = $"Finished {result.Registration.RaceDistance.Race.Name} - Rank #{result.OverallRank}",
                        ActivityDate = result.Registration.RaceDistance.Race.RaceDate,
                        Icon = "fa-flag-checkered",
                        BadgeClass = result.OverallRank <= 3 ? "bg-warning" : "bg-success"
                    });
                }

                recentActivities = recentActivities.OrderByDescending(a => a.ActivityDate).Take(10).ToList();

                // Get personal records (best time for each distance)
                var personalRecords = finishedResults
                    .GroupBy(r => new
                    {
                        r.Registration.RaceDistance.DistanceInKm,
                        r.Registration.RaceDistance.Name
                    })
                    .Select(g =>
                    {
                        var bestResult = g.OrderBy(r => r.CompletionTime).First();
                        return new PersonalRecordDto
                        {
                            DistanceName = g.Key.Name,
                            DistanceInKm = g.Key.DistanceInKm,
                            BestTime = bestResult.CompletionTime!.Value,
                            FormattedTime = bestResult.CompletionTime!.Value.ToString(@"hh\:mm\:ss"),
                            RaceName = bestResult.Registration.RaceDistance.Race.Name,
                            RaceDate = bestResult.Registration.RaceDistance.Race.RaceDate,
                            AveragePace = $"{(bestResult.CompletionTime.Value.ToTimeSpan().TotalMinutes / (double)g.Key.DistanceInKm):F2} min/km"
                        };
                    })
                    .OrderBy(pr => pr.DistanceInKm)
                    .ToList();

                var profile = new RunnerProfileDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Age = age,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Statistics = statistics,
                    RecentActivities = recentActivities,
                    PersonalRecords = personalRecords
                };

                return Ok(new ApiResponse<RunnerProfileDto>
                {
                    Success = true,
                    Data = profile
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<RunnerProfileDto>
                {
                    Success = false,
                    Message = "An error occurred while fetching profile",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
        /// <summary>
        /// POST: api/Races/runner/registrations/{registrationId}/fake-result
        /// Generate a fake result for the given registration (FOR TESTING ONLY)
        /// </summary>
        [HttpPost("runner/registrations/{registrationId}/fake-result")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<MyResultDto>>> GenerateFakeResult(int registrationId)
        {
            var userId = GetCurrentUserId();

            try
            {
                // 1. Lấy registration thuộc về runner hiện tại
                var registration = await _context.Registrations
                    .Include(r => r.RaceDistance)
                        .ThenInclude(rd => rd.Race)
                    .FirstOrDefaultAsync(r => r.Id == registrationId && r.RunnerId == userId);

                if (registration == null)
                {
                    return NotFound(new ApiResponse<MyResultDto>
                    {
                        Success = false,
                        Message = "Registration not found for current runner"
                    });
                }

                // 2. Cho chắc: chỉ fake khi race đã diễn ra (hoặc bạn có thể bỏ check này nếu muốn test tự do)
                //if (registration.RaceDistance.Race.RaceDate > DateTime.Now)
                //{
                    //return BadRequest(new ApiResponse<MyResultDto>
                   // {
                      //  Success = false,
                      //  Message = "Race has not occurred yet, cannot generate result"
                   // });
              //  }

                // 3. Lấy (hoặc tạo mới) Result cho registration này
                var result = await _context.Results
                    .Include(res => res.Registration)
                        .ThenInclude(reg => reg.RaceDistance)
                        .ThenInclude(rd => rd.Race)
                    .FirstOrDefaultAsync(res => res.RegistrationId == registrationId);

                if (result == null)
                {
                    result = new Result
                    {
                        RegistrationId = registration.Id
                    };
                    _context.Results.Add(result);
                }

                // 4. Sinh dữ liệu giả
                var random = new Random();
                var distanceKm = (double)registration.RaceDistance.DistanceInKm;

                // Pace trung bình 4.5–8.0 phút/km
                var paceMinPerKm = 4.5 + random.NextDouble() * 3.5;
                var totalMinutes = paceMinPerKm * distanceKm;

                // +/- thêm tối đa 5 phút cho tự nhiên
                totalMinutes += (random.NextDouble() * 10.0) - 5.0;

                // Không cho nhanh hơn 3 phút/km
                var minMinutes = distanceKm * 3.0;
                if (totalMinutes < minMinutes)
                    totalMinutes = minMinutes;

                var timeSpan = TimeSpan.FromMinutes(totalMinutes);
                var completionTime = new TimeOnly(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);

                // Rank ngẫu nhiên (1–500)
                var overallRank = random.Next(1, 501);
                var genderRank = random.Next(1, 501);
                var ageRank = random.Next(1, 501);

                // 5. Gán dữ liệu vào Result – status phải là 1 trong: DidNotStart | Finished | DNF
                result.CompletionTime = completionTime;
                result.OverallRank = overallRank;
                result.GenderRank = genderRank;
                result.AgeCategoryRank = ageRank;
                result.Status = "Finished";

                await _context.SaveChangesAsync();

                // 6. Build MyResultDto (match với MyResultDto trong RunnerDTOs.cs)
                var dto = new MyResultDto
                {
                    Id = result.Id,
                    RegistrationId = result.RegistrationId,
                    CompletionTime = result.CompletionTime,
                    OverallRank = result.OverallRank,
                    GenderRank = result.GenderRank,
                    AgeCategoryRank = result.AgeCategoryRank,
                    Status = result.Status,

                    RaceId = registration.RaceDistance.RaceId,
                    RaceName = registration.RaceDistance.Race.Name,
                    Location = registration.RaceDistance.Race.Location,
                    RaceDate = registration.RaceDistance.Race.RaceDate,

                    DistanceName = registration.RaceDistance.Name,
                    DistanceInKm = registration.RaceDistance.DistanceInKm,

                    // 2 field view của bạn đang dùng
                    FormattedTime = result.CompletionTime?.ToString(@"hh\:mm\:ss"),
                    AveragePace = result.CompletionTime.HasValue
                        ? $"{result.CompletionTime.Value.ToTimeSpan().TotalMinutes / (double)registration.RaceDistance.DistanceInKm:F1} min/km"
                        : null
                };

                return Ok(new ApiResponse<MyResultDto>
                {
                    Success = true,
                    Message = "Fake result generated successfully",
                    Data = dto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<MyResultDto>
                {
                    Success = false,
                    Message = "An error occurred while generating fake result",
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
                });
            }
        }


        /// <summary>
        /// PUT: api/Races/runner/profile
        /// Update runner profile
        /// </summary>
        [HttpPut("runner/profile")]
        [Authorize(Roles = "Runner")]
        public async Task<ActionResult<ApiResponse<RunnerProfileDto>>> UpdateRunnerProfile(
            [FromBody] UpdateRunnerProfileRequest request)
        {
            var userId = GetCurrentUserId();

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new ApiResponse<RunnerProfileDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });

                // Update fields
                user.FullName = request.FullName;
                user.PhoneNumber = request.PhoneNumber;
                user.DateOfBirth = request.DateOfBirth;
                user.Gender = request.Gender;

                await _context.SaveChangesAsync();

                // Return updated profile
                var profileResponse = await GetRunnerProfile();
                return Ok(new ApiResponse<RunnerProfileDto>
                {
                    Success = true,
                    Message = "Profile updated successfully",
                    Data = profileResponse.Value?.Data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<RunnerProfileDto>
                {
                    Success = false,
                    Message = "An error occurred while updating profile",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}