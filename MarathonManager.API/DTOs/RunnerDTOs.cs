using System;

namespace MarathonManager.API.DTOs
{
    // ============================================
    // RUNNER DASHBOARD DTOs
    // ============================================

    /// <summary>
    /// Main dashboard view model for runners
    /// </summary>
    public class RunnerDashboardDto
    {
        public RunnerStatisticsDto Statistics { get; set; } = new();
        public List<AvailableRaceDto> AvailableRaces { get; set; } = new();
        public List<MyRegistrationDto> MyRegistrations { get; set; } = new();
        public List<MyResultDto> MyResults { get; set; } = new();
    }

    /// <summary>
    /// Statistics for runner dashboard
    /// </summary>
    public class RunnerStatisticsDto
    {
        public int TotalRegistrations { get; set; }
        public int CompletedRaces { get; set; }
        public int UpcomingRaces { get; set; }
        public int PendingRegistrations { get; set; }
    }

    // ============================================
    // TAB 1: AVAILABLE RACES DTOs
    // ============================================

    /// <summary>
    /// DTO for displaying available races that runners can register for
    /// </summary>
    public class AvailableRaceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Location { get; set; } = string.Empty;
        public DateTime RaceDate { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;

        // Organizer info
        public int OrganizerId { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public string? OrganizerEmail { get; set; }

        // Race distances available
        public List<RaceDistanceSummaryDto> Distances { get; set; } = new();

        // Check if current user already registered
        public bool IsAlreadyRegistered { get; set; }

        // For display
        public int TotalParticipants { get; set; }
        public int AvailableSlots { get; set; }
    }

    /// <summary>
    /// Summary of race distance options
    /// </summary>
    public class RaceDistanceSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DistanceInKm { get; set; }
        public decimal RegistrationFee { get; set; }
        public int MaxParticipants { get; set; }
        public DateTime StartTime { get; set; }
        public int CurrentParticipants { get; set; }
        public bool IsFull { get; set; }
    }

    // ============================================
    // TAB 2: MY REGISTRATIONS DTOs
    // ============================================

    /// <summary>
    /// DTO for runner's registration list
    /// </summary>
    public class MyRegistrationDto
    {
        public int Id { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? BibNumber { get; set; }

        // Race info
        public int RaceId { get; set; }
        public string RaceName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime RaceDate { get; set; }
        public string? RaceImageUrl { get; set; }

        // Distance info
        public int RaceDistanceId { get; set; }
        public string DistanceName { get; set; } = string.Empty;
        public decimal DistanceInKm { get; set; }
        public decimal RegistrationFee { get; set; }
        public DateTime StartTime { get; set; }

        // Status helpers
        public bool CanCancel { get; set; }
        public bool HasResult { get; set; }
        public string DisplayStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed registration view
    /// </summary>
    public class RegistrationDetailDto
    {
        public int Id { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? BibNumber { get; set; }

        // Full race details
        public RaceDetailsDto Race { get; set; } = new();

        // Full distance details
        public RaceDistanceDetailDto RaceDistance { get; set; } = new();

        // Runner info
        public RunnerInfoDto Runner { get; set; } = new();

        // Result if exists
        public MyResultDto? Result { get; set; }
    }

    // ============================================
    // TAB 3: MY RESULTS DTOs
    // ============================================

    /// <summary>
    /// DTO for displaying runner's race results
    /// </summary>
    public class MyResultDto
    {
        public int Id { get; set; }
        public int RegistrationId { get; set; }
        public TimeOnly? CompletionTime { get; set; }
        public int? OverallRank { get; set; }
        public int? GenderRank { get; set; }
        public int? AgeCategoryRank { get; set; }
        public string Status { get; set; } = string.Empty;

        // Race info
        public int RaceId { get; set; }
        public string RaceName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime RaceDate { get; set; }

        // Distance info
        public string DistanceName { get; set; } = string.Empty;
        public decimal DistanceInKm { get; set; }

        // Calculated fields
        public string? FormattedTime { get; set; }
        public string? AveragePace { get; set; } // min/km
        public bool IsTopThree => OverallRank.HasValue && OverallRank <= 3;
        public string MedalIcon => OverallRank switch
        {
            1 => "🥇",
            2 => "🥈",
            3 => "🥉",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Detailed result view with leaderboard
    /// </summary>
    public class ResultDetailDto
    {
        public MyResultDto MyResult { get; set; } = new();
        public List<LeaderboardEntryDto> Leaderboard { get; set; } = new();
        public RaceDetailsDto Race { get; set; } = new();
    }

    /// <summary>
    /// Leaderboard entry for result detail view
    /// </summary>
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public string RunnerName { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public TimeOnly? CompletionTime { get; set; }
        public string? FormattedTime { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    // ============================================
    // SUPPORTING DTOs
    // ============================================

    /// <summary>
    /// Detailed race information
    /// </summary>
    public class RaceDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Location { get; set; } = string.Empty;
        public DateTime RaceDate { get; set; }
        public string? ImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Organizer
        public int OrganizerId { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public string? OrganizerEmail { get; set; }
        public string? OrganizerPhone { get; set; }

        // Distances
        public List<RaceDistanceDetailDto> Distances { get; set; } = new();

        // Blog posts related to this race
        public List<BlogListItemDto> BlogPosts { get; set; } = new();
    }

    /// <summary>
    /// Detailed race distance information
    /// </summary>
    public class RaceDistanceDetailDto
    {
        public int Id { get; set; }
        public int RaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DistanceInKm { get; set; }
        public decimal RegistrationFee { get; set; }
        public int MaxParticipants { get; set; }
        public DateTime StartTime { get; set; }
        public int CurrentParticipants { get; set; }
        public int AvailableSlots => MaxParticipants - CurrentParticipants;
        public bool IsFull => CurrentParticipants >= MaxParticipants;
        public decimal PercentageFilled => MaxParticipants > 0
            ? (decimal)CurrentParticipants / MaxParticipants * 100
            : 0;
    }

    /// <summary>
    /// Runner information
    /// </summary>
    public class RunnerInfoDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public int? Age { get; set; }
    }

    /// <summary>
    /// Blog post summary for race details
    /// </summary>
    public class BlogPostSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? FeaturedImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ============================================
    // REQUEST DTOs (for POST actions)
    // ============================================

    /// <summary>
    /// Request to register for a race
    /// </summary>
    public class RegisterForRaceRequest
    {
        public int RaceId { get; set; }
        public int RaceDistanceId { get; set; }
    }

    /// <summary>
    /// Request to cancel a registration
    /// </summary>
    public class CancelRegistrationRequest
    {
        public int RegistrationId { get; set; }
        public string? Reason { get; set; }
    }

    // ============================================
    // RESPONSE DTOs
    // ============================================

    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Paginated response
    /// </summary>
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class RunnerProfileDto
    {
        // Personal Information
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Profile Statistics
        public RunnerProfileStatisticsDto Statistics { get; set; } = new();

        // Recent Activity
        public List<RecentActivityDto> RecentActivities { get; set; } = new();

        // Personal Records
        public List<PersonalRecordDto> PersonalRecords { get; set; } = new();
    }

    /// <summary>
    /// Statistics for runner profile
    /// </summary>
    public class RunnerProfileStatisticsDto
    {
        // Registration Stats
        public int TotalRegistrations { get; set; }
        public int ActiveRegistrations { get; set; }
        public int CompletedRaces { get; set; }
        public int CancelledRegistrations { get; set; }

        // Performance Stats
        public int TotalRacesFinished { get; set; }
        public decimal TotalDistanceRun { get; set; } // in KM
        public int Top3Finishes { get; set; }
        public int Top10Finishes { get; set; }

        // Best Times (optional - if available)
        public TimeOnly? Best5K { get; set; }
        public TimeOnly? Best10K { get; set; }
        public TimeOnly? BestHalfMarathon { get; set; }
        public TimeOnly? BestMarathon { get; set; }

        // Account Age
        public int DaysSinceJoined { get; set; }
        public int YearsActive { get; set; }
    }

    /// <summary>
    /// Recent activity item
    /// </summary>
    public class RecentActivityDto
    {
        public string ActivityType { get; set; } = string.Empty; // "Registration", "Race", "Result"
        public string Description { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; }
        public string? Icon { get; set; }
        public string? BadgeClass { get; set; }
    }

    /// <summary>
    /// Personal record for specific distance
    /// </summary>
    public class PersonalRecordDto
    {
        public string DistanceName { get; set; } = string.Empty;
        public decimal DistanceInKm { get; set; }
        public TimeOnly BestTime { get; set; }
        public string FormattedTime { get; set; } = string.Empty;
        public string RaceName { get; set; } = string.Empty;
        public DateTime RaceDate { get; set; }
        public string AveragePace { get; set; } = string.Empty;
    }

    /// <summary>
    /// Update runner profile request
    /// </summary>
    public class UpdateRunnerProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
    }

    /// <summary>
    /// Change password request
    /// </summary>
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class BlogListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Excerpt { get; set; } = default!;
        public string? FeaturedImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; } = default!;
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }

        // liên kết race (optional)
        public int? RaceId { get; set; }
        public string? RaceName { get; set; }
    }

    public class BlogDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string? FeaturedImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; } = default!;
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<CommentDto> Comments { get; set; } = new();

        // liên kết race (optional)
        public int? RaceId { get; set; }
        public string? RaceName { get; set; }
        public DateTime? RaceDate { get; set; }
    }


    public class CommentDto
    {
        public int Id { get; set; }
        public int BlogPostId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = default!;
        public string Content { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCommentRequest
    {
        public string Content { get; set; } = default!;
    }

    public class ToggleLikeResponse
    {
        public bool Liked { get; set; }
        public int LikeCount { get; set; }
    }
}
